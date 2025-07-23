Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json
Imports System.Security.Cryptography
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
            Dim payload As New With {
                .hardware_id = Globals.hardwareId,
                .name = Environment.MachineName ' Machine Name
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
End Class
