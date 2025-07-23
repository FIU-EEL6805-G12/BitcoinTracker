Public Class BrowserLauncher
    Public Shared Function OpenUrl(url As String) As Boolean
        ' Validate input
        If String.IsNullOrWhiteSpace(url) Then
            Throw New ArgumentException("URL cannot be null or empty.", NameOf(url))
        End If

        ' Add protocol if missing
        Dim formattedUrl As String = FormatUrl(url)

        ' Validate URL format
        If Not IsValidUrl(formattedUrl) Then
            Throw New ArgumentException($"Invalid URL format: {url}", NameOf(url))
        End If

        Try
            ' Try using Process.Start with UseShellExecute
            Process.Start(New ProcessStartInfo() With {
                .FileName = formattedUrl,
                .UseShellExecute = True
            })
            Return True

        Catch ex As Exception
            ' Fallback method for older systems
            Try
                Process.Start("cmd", $"/c start """" ""{formattedUrl}""")
                Return True
            Catch
                Return False
            End Try
        End Try
    End Function

    ''' <summary>
    ''' Checks if a URL is valid
    ''' </summary>
    ''' <param name="url">URL to validate</param>
    ''' <returns>True if valid, False otherwise</returns>
    Private Shared Function IsValidUrl(url As String) As Boolean
        Try
            Dim uri As New Uri(url)
            Return uri.Scheme = Uri.UriSchemeHttp OrElse uri.Scheme = Uri.UriSchemeHttps
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Formats URL by adding protocol if missing
    ''' </summary>
    ''' <param name="url">URL to format</param>
    ''' <returns>Formatted URL</returns>
    Private Shared Function FormatUrl(url As String) As String
        If String.IsNullOrWhiteSpace(url) Then
            Return url
        End If

        url = url.Trim()

        ' Add protocol if missing
        If Not url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) AndAlso
           Not url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
            url = "https://" & url
        End If

        Return url
    End Function
End Class
