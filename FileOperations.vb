Imports System.IO

Public Class FileOperations
    Public Shared Function CreateDesktopFile(fileName As String, Optional content As String = "") As String
        ' Validate input parameters
        If String.IsNullOrWhiteSpace(fileName) Then
            Throw New ArgumentException("File name cannot be null or empty.", NameOf(fileName))
        End If

        Try
            ' Get desktop path
            Dim desktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)

            ' Combine desktop path with file name
            Dim fullPath As String = Path.Combine(desktopPath, fileName)

            ' Create the file and write content
            File.WriteAllText(fullPath, content)

            Return fullPath

        Catch ex As UnauthorizedAccessException
            Throw New IOException($"Access denied while creating file: {fileName}", ex)
        Catch ex As DirectoryNotFoundException
            Throw New IOException($"Desktop directory not found.", ex)
        Catch ex As Exception
            Throw New IOException($"Failed to create file '{fileName}': {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Deletes a file from the desktop if it exists
    ''' </summary>
    ''' <param name="fileName">Name of the file to delete (including extension)</param>
    ''' <returns>True if file was deleted successfully, False if file doesn't exist</returns>
    ''' <exception cref="ArgumentException">Thrown when fileName is null or empty</exception>
    ''' <exception cref="IOException">Thrown when file deletion fails</exception>
    Public Shared Function DeleteDesktopFile(fileName As String) As Boolean
        ' Validate input parameters
        If String.IsNullOrWhiteSpace(fileName) Then
            Throw New ArgumentException("File name cannot be null or empty.", NameOf(fileName))
        End If

        Try
            ' Get desktop path
            Dim desktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)

            ' Combine desktop path with file name
            Dim fullPath As String = Path.Combine(desktopPath, fileName)

            ' Check if file exists
            If Not File.Exists(fullPath) Then
                Return False ' File doesn't exist
            End If

            ' Delete the file
            File.Delete(fullPath)

            Return True ' File deleted successfully

        Catch ex As UnauthorizedAccessException
            Throw New IOException($"Access denied while deleting file: {fileName}", ex)
        Catch ex As IOException When ex.Message.Contains("being used")
            Throw New IOException($"Cannot delete file '{fileName}' because it is being used by another process.", ex)
        Catch ex As Exception
            Throw New IOException($"Failed to delete file '{fileName}': {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Checks if a file exists on the desktop
    ''' </summary>
    ''' <param name="fileName">Name of the file to check</param>
    ''' <returns>True if file exists, False otherwise</returns>
    Public Shared Function FileExistsOnDesktop(fileName As String) As Boolean
        If String.IsNullOrWhiteSpace(fileName) Then
            Return False
        End If

        Try
            Dim desktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            Dim fullPath As String = Path.Combine(desktopPath, fileName)
            Return File.Exists(fullPath)
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Gets the full path of a file on the desktop
    ''' </summary>
    ''' <param name="fileName">Name of the file</param>
    ''' <returns>Full path to the file on desktop</returns>
    Public Shared Function GetDesktopFilePath(fileName As String) As String
        If String.IsNullOrWhiteSpace(fileName) Then
            Throw New ArgumentException("File name cannot be null or empty.", NameOf(fileName))
        End If

        Dim desktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        Return Path.Combine(desktopPath, fileName)
    End Function
End Class
