Imports System.Net.Http

Public Class ApiClient
    Private Shared ReadOnly _httpClient As New HttpClient()

    Public Shared ReadOnly Property Client As HttpClient
        Get
            Return _httpClient
        End Get
    End Property
End Class