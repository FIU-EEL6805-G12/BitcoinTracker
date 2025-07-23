Imports NAudio.CoreAudioApi

Public Class AudioManager
    Public Shared Sub SetMute(status As Boolean)
        Dim dev = New MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
        dev.AudioEndpointVolume.Mute = status
    End Sub
End Class