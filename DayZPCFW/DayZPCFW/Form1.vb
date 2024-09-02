Imports System.Runtime.InteropServices
Imports System.Timers
Imports System.IO

Public Class Form1
    Private WithEvents Timer1 As New Timers.Timer
    Private RuleName As String = "Lagger"
    Private ExeLocation As String
    Private Hotkey As UInteger
    Public FounddayzPath As String = nothing

    ' Windows API functions for registering and unregistering hotkeys
    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function

    Private Const HOTKEY_ID As Integer = 1
    Private Const MOD_NONE As UInteger = &H0
    Private Function GetDayzPath() As String
        Dim steamPath As String = FindSteamPath()

        If Not String.IsNullOrEmpty(steamPath) Then
            Dim steamAppsPath As String = Path.Combine(steamPath, "steamapps")
            Dim DayZPath As String = steamAppsPath & "\common\DayZ\DayZ_x64.exe"

            If System.IO.File.Exists(DayZPath) Then
                Return DayZPath
            Else
                Return ""
            End If
            Return DayZPath
        Else
            Return ""
        End If
        Return ""

    End Function

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load


        Dim dayzPath As String = GetDayzPath()
        If (GetDayzPath() IsNot "") Then
            FounddayzPath = dayzPath
        Else
            FounddayzPath = "C:\FindYourSteamAppFolder\steamapps\common\DayZ\DayZ_x64.exe"
        End If



        ' Ensure the configuration file exists
        EnsureConfigFileExists()

        ' Load settings from configuration file
        LoadSettings()
        ' Register the hotkey and check if it was successful
        RegisterHotKey(Me.Handle, HOTKEY_ID, MOD_NONE, Hotkey)

        ' Populate ComboBox with time options in seconds
        ComboBox1.Items.AddRange(New String() {"Trigger", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"})
        ComboBox1.SelectedIndex = 0  ' Default to the first item

        Timer1.AutoReset = False  ' Timer should run only once
        EnsureFirewallRulesExist()  ' Ensure both inbound and outbound "Lagger" rules exist
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Unregister the hotkey when the form is closing
        UnregisterHotKey(Me.Handle, HOTKEY_ID)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ToggleFirewallRules()
    End Sub

    Private Sub Timer1_Elapsed(sender As Object, e As ElapsedEventArgs) Handles Timer1.Elapsed
        Button1.Text = "Start"
        DisableFirewallRules()
    End Sub

    Private Sub EnsureFirewallRulesExist()
        ' Check and create the inbound rule if it doesn't exist
        Dim inboundCommand As String = $"netsh advfirewall firewall show rule name=""{RuleName}_Inbound"""
        If Not ExecuteCommandAndCheckOutput(inboundCommand, "No rules match the specified criteria.") Then
            inboundCommand = $"netsh advfirewall firewall add rule name=""{RuleName}_Inbound"" dir=in action=block program=""" & ExeLocation & """ enable=no"
            ExecuteCommand(inboundCommand)
        End If

        ' Check and create the outbound rule if it doesn't exist
        Dim outboundCommand As String = $"netsh advfirewall firewall show rule name=""{RuleName}_Outbound"""
        If Not ExecuteCommandAndCheckOutput(outboundCommand, "No rules match the specified criteria.") Then
            outboundCommand = $"netsh advfirewall firewall add rule name=""{RuleName}_Outbound"" dir=out action=block program=""" & ExeLocation & """ enable=no"
            ExecuteCommand(outboundCommand)
        End If
    End Sub

    Private Sub EnableFirewallRules()
        ' Enable the inbound rule
        Dim inboundCommand As String = $"netsh advfirewall firewall set rule name=""{RuleName}_Inbound"" new enable=yes"
        ExecuteCommand(inboundCommand)

        ' Enable the outbound rule
        Dim outboundCommand As String = $"netsh advfirewall firewall set rule name=""{RuleName}_Outbound"" new enable=yes"
        ExecuteCommand(outboundCommand)
    End Sub

    Private Sub DisableFirewallRules()
        ' Disable the inbound rule
        Dim inboundCommand As String = $"netsh advfirewall firewall set rule name=""{RuleName}_Inbound"" new enable=no"
        ExecuteCommand(inboundCommand)

        ' Disable the outbound rule
        Dim outboundCommand As String = $"netsh advfirewall firewall set rule name=""{RuleName}_Outbound"" new enable=no"
        ExecuteCommand(outboundCommand)
    End Sub
    Private Sub ExecuteCommand(command As String)
        Dim process As New Process()
        Dim startInfo As New ProcessStartInfo() With {
        .FileName = "cmd.exe",
        .Arguments = $"/c {command}",
        .WindowStyle = ProcessWindowStyle.Hidden,
        .CreateNoWindow = True,
        .UseShellExecute = False,  ' Ensure no window is created
        .Verb = "runas"  ' Run as administrator
    }
        process.StartInfo = startInfo
        process.Start()
        process.WaitForExit()
    End Sub

    Private Function ExecuteCommandAndCheckOutput(command As String, searchText As String) As Boolean
        Dim process As New Process()
        Dim startInfo As New ProcessStartInfo() With {
        .FileName = "cmd.exe",
        .Arguments = $"/c {command}",
        .RedirectStandardOutput = True,
        .UseShellExecute = False,  ' Ensure no window is created
        .CreateNoWindow = True
    }
        process.StartInfo = startInfo
        process.Start()

        Dim output As String = process.StandardOutput.ReadToEnd()
        process.WaitForExit()

        Return Not output.Contains(searchText)
    End Function
    ' Override the WndProc method to capture the hotkey press
    Protected Overrides Sub WndProc(ByRef m As Message)
        Const WM_HOTKEY As Integer = &H312
        If m.Msg = WM_HOTKEY Then
            If m.WParam.ToInt32() = HOTKEY_ID Then
                ToggleFirewallRules()
            End If
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub ToggleFirewallRules()
        If Button1.Text = "Start" Then
            Button1.Text = "Stop"
            If ComboBox1.SelectedItem = "Trigger" Then
                EnableFirewallRules()
            Else
                Dim selectedSeconds As Integer = Convert.ToInt32(ComboBox1.SelectedItem)
                EnableFirewallRules()
                Timer1.Interval = selectedSeconds * 1000
                Timer1.Start()
            End If
        Else
            Button1.Text = "Start"
            DisableFirewallRules()
        End If
    End Sub

    Private Sub LoadSettings()
        Try
            Dim configFilePath As String = "settings.cfg"
            If File.Exists(configFilePath) Then
                Dim lines() As String = File.ReadAllLines(configFilePath)
                For Each line As String In lines
                    Dim parts() As String = line.Split("="c)
                    If parts.Length = 2 Then
                        Select Case parts(0).Trim()
                            Case "ExeLocation"
                                ExeLocation = parts(1).Trim()
                            Case "Hotkey"
                                Hotkey = ConvertHotkey(parts(1).Trim())
                        End Select
                    End If
                Next
            Else
                MessageBox.Show("Configuration file not found.")
            End If
        Catch ex As Exception
            MessageBox.Show("Error loading configuration: " & ex.Message)
        End Try
    End Sub
    Private Function ConvertHotkey(hotkey As String) As UInteger
        Try
            Select Case hotkey.ToUpper()
                Case "SPACE"
                    Return &H20
                Case "F1"
                    Return &H70
                Case "F2"
                    Return &H71
                Case "F3"
                    Return &H72
                Case "F4"
                    Return &H73
                Case "F5"
                    Return &H74
                Case "F6"
                    Return &H75
                Case "F7"
                    Return &H76
                Case "F8"
                    Return &H77
                Case "F9"
                    Return &H78
                Case "F10"
                    Return &H79
                Case "F11"
                    Return &H7A
                Case "F12"
                    Return &H7B
                Case Else
                    Dim key As Keys = [Enum].Parse(GetType(Keys), hotkey, True)
                    Return CUInt(key)
            End Select


        Catch ex As Exception
            Return &H0 ' Default to no hotkey if conversion fails
        End Try
    End Function
    Private Sub EnsureConfigFileExists()
        Dim configFilePath As String = "settings.cfg"

        ' Check if the configuration file exists
        If Not File.Exists(configFilePath) Then
            ' Create the configuration file with default settings
            Dim defaultConfig As String = "[Settings]" & Environment.NewLine &
                                           "ExeLocation=" & FounddayzPath & Environment.NewLine &
                                           "Hotkey=X"

            ' Write the default settings to the configuration file
            File.WriteAllText(configFilePath, defaultConfig)
        End If
    End Sub

    Function IsRuleRelatedToExecutable(programPath As String, executableName As String) As Boolean
        ' Check if the filename matches the specified executable name
        Return programPath.ToLower().Contains(executableName.ToLower())
    End Function
    Sub DisableAnyExistingRules()
        Dim executableName As String = "DayZ_x64.exe"
        Try
            ' Execute the netsh command to list all firewall rules
            Dim psi As New ProcessStartInfo("netsh", "advfirewall firewall show rule name=all")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            Dim process As New Process()
            process.StartInfo = psi
            process.Start()

            ' Read and process the command output line by line
            Dim output As String = process.StandardOutput.ReadToEnd()
            process.WaitForExit()

            Dim ruleNames As New List(Of String)()
            Dim currentRuleName As String = String.Empty
            Dim lines As String() = output.Split(New String() {Environment.NewLine}, StringSplitOptions.None)

            For Each line As String In lines
                If line.Contains("Rule Name:") Then
                    currentRuleName = line.Substring(line.IndexOf(":") + 1).Trim()
                ElseIf line.Contains("Program:") AndAlso Not String.IsNullOrEmpty(currentRuleName) Then
                    Dim programPath As String = line.Substring(line.IndexOf(":") + 1).Trim()
                    If IsRuleRelatedToExecutable(programPath, executableName) Then
                        ruleNames.Add(currentRuleName)
                    End If
                    currentRuleName = String.Empty ' Reset for next rule
                End If
            Next

            ' Disable matching rules
            For Each RuleName In ruleNames
                Console.WriteLine($"Disabling rule: {RuleName}")
                Dim disableProcess As New ProcessStartInfo("netsh", $"advfirewall firewall delete rule name=""{RuleName}""")
                disableProcess.UseShellExecute = False
                disableProcess.CreateNoWindow = True

                Dim disableProc As New Process()
                disableProc.StartInfo = disableProcess
                disableProc.Start()
                disableProc.WaitForExit()
            Next

            Console.WriteLine("Processing complete.")
        Catch ex As Exception
            Console.WriteLine($"An error occurred: {ex.Message}")
        End Try
    End Sub

    Function FindSteamPath() As String
        Dim defaultPaths As String() = {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
        }

        For Each path In defaultPaths
            If Directory.Exists(path) Then
                Return path
            End If
        Next

        ' If not found, you might consider searching for the Steam folder or reading the registry
        ' Example for searching within the user profile directory:
        Dim possiblePath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam")
        If Directory.Exists(possiblePath) Then
            Return possiblePath
        End If

        ' Add any other custom paths or search logic here
        Return String.Empty
    End Function
End Class
