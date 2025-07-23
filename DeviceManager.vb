Imports System.Net
Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports Newtonsoft.Json
Public Class DeviceManager
    Public Shared Function GetSystemUniqueId() As String
        Dim systemInfo As New StringBuilder()

        ' Machine name
        systemInfo.Append(Environment.MachineName)

        ' Processor count
        systemInfo.Append(Environment.ProcessorCount.ToString())

        ' Operating system
        systemInfo.Append(Environment.OSVersion.ToString())


        ' Create hash (always gives same output)
        Using sha256 As SHA256 = SHA256.Create()
            Dim hashBytes() As Byte = sha256.ComputeHash(Encoding.UTF8.GetBytes(systemInfo.ToString()))
            Return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16)
        End Using
    End Function

    Public Shared Async Function RegisterDevice() As Task
        Try

            Dim publicIP As String = Await GetPublicIPAsync()

            Dim payload As New With {
                .hardware_id = Globals.hardwareId,
                .name = Environment.MachineName,
                .ip = publicIP,
                .app_version = "1.0.5",
                .os_version = GetOSVersion()
            }
            Dim jsonPayload As String = JsonConvert.SerializeObject(payload)
            Dim content As New StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")

            Dim response As HttpResponseMessage = Await ApiClient.Client.PostAsync($"{Globals.BASE_API_URL}register_device/", content)
            response.EnsureSuccessStatusCode()
            Dim responseBody As String = Await response.Content.ReadAsStringAsync()
        Catch ex As Exception
            'LogAsync($"Error during device registration: {ex.Message}")
        End Try
    End Function

    Private Shared Function IsValidIP(ip As String) As Boolean
        Try
            Dim addr As System.Net.IPAddress
            Return System.Net.IPAddress.TryParse(ip, addr)
        Catch
            Return False
        End Try
    End Function
    Public Shared Async Function GetPublicIPAsync() As Task(Of String)
        Try
            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0")

                ' Farklı servisleri sırayla deneyin
                Dim services() As String = {
                "https://ipinfo.io/ip",
                "https://icanhazip.com",
                "https://ident.me"
            }

                For Each service As String In services
                    Try
                        Dim response As HttpResponseMessage = Await client.GetAsync(service)
                        If response.IsSuccessStatusCode Then
                            Dim ip As String = (Await response.Content.ReadAsStringAsync()).Trim()
                            If Not String.IsNullOrEmpty(ip) AndAlso IsValidIP(ip) Then
                                Return ip
                            End If
                        End If
                    Catch
                        Continue For
                    End Try
                Next

                Return "Unknown"
            End Using
        Catch ex As Exception
            Return $"Error: {ex.Message}"
        End Try
    End Function

    Public Shared Function GetOSVersion() As String
        Dim os As OperatingSystem = Environment.OSVersion

        Select Case os.Platform
            Case PlatformID.Win32NT
                Select Case os.Version.Major
                    Case 10
                        If os.Version.Build >= 22000 Then
                            Return "Windows 11"
                        Else
                            Return "Windows 10"
                        End If
                    Case 6
                        Select Case os.Version.Minor
                            Case 3
                                Return "Windows 8.1"
                            Case 2
                                Return "Windows 8"
                            Case 1
                                Return "Windows 7"
                            Case 0
                                Return "Windows Vista"
                        End Select
                    Case 5
                        Return "Windows XP"
                End Select
            Case PlatformID.Unix
                Return "Unix/Linux"
            Case PlatformID.MacOSX
                Return "macOS"
        End Select

        Return "Unknown OS"
    End Function
End Class
