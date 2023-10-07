Imports VBLib
Imports Newtonsoft.Json.Linq
Imports System.IO

Module main
    Private scan_init_response As JObject

    Sub Main()
        Console.WriteLine(" ______ ____    ____ .______    _______. ______      .__   __.   ______    " & vbCrLf &
"/      |\   \  /   / |   _  \  |   ____||   _  \     |  \ |  |  /  __  \   " & vbCrLf &
"| ,----' \   \/   /  |  |_)  | |  |__   |  |_)  |    |   \|  | |  |  |  |  " & vbCrLf &
"| |       \_    _/   |   _  <  |   __|  |      /     |. ` |  | |  |  |  |  " & vbCrLf &
"| `----.    |  |     |  |_)  | |  |____ |  |\  \----.|  |\   | |  `--'  |  " & vbCrLf &
"\______|    |__|     |______/  |_______|| _| `._____||__| \__|  \______/   " & vbCrLf)
        Dim username As String = "", password As String = "", serveraddress As String = ""
        Dim file_path As String = ""

        Console.Write("Please insert API server address [Default=https://multiscannerdemo.cyberno.ir/]: ")
        serveraddress = Console.ReadLine()
        If serveraddress = "" Then
            serveraddress = "https://multiscannerdemo.cyberno.ir/"
        End If
        If serveraddress.EndsWith("/") = False Then
            serveraddress = serveraddress + "/"
        End If

        Console.Write("Please insert identifier (email): ")
        username = Console.ReadLine()

        Console.Write("Please insert your password: ")
        password = Console.ReadLine()

        Dim cyutils As CyUtils = New CyUtils(serveraddress)
        Dim params1 As JObject = New JObject
        params1.Add("email", username)
        params1.Add("password", password)
        Dim return_value As JObject = cyutils.call_with_json_input("user/login", params1)
        If (return_value.Item("success").ToObject(Of Boolean) = True) Then
            Console.Write("You are logged in successfully." + vbCrLf)
        Else
            Console.Write(cyutils.get_error(return_value))
            Console.ReadLine()
            Return
        End If

        Dim apikey = return_value.Item("data").ToString
        Console.WriteLine("Please select scan mode:")
        Console.WriteLine("1- Scan local folder")
        Console.WriteLine("2- Scan file")
        Console.Write("Enter Number=")
        Dim Index = Console.ReadLine()
        If Index = 1 Then
            'prompt user to enter file paths
            Dim paths As String = ""
            Dim sentenceTwo As String = ""
            Console.WriteLine("Please enter the paths of file to scan (with spaces): ")
            paths = Console.ReadLine()
            sentenceTwo = paths
            Dim file_path_array As String() = sentenceTwo.Split(" ")

            'prompt user to enter antivirus names
            Dim avsSentenceTwo As String = ""
            Console.WriteLine("Enter the name of the selected antivirus (with spaces): ")
            avsSentenceTwo = Console.ReadLine()
            sentenceTwo = paths
            Dim avs_array As String() = avsSentenceTwo.Split(" ")

            'make Json item
            Dim scan_item_json As JObject = New JObject()
            scan_item_json.Add("token", apikey)
            scan_item_json.Add("paths", New JArray(file_path_array))
            scan_item_json.Add("avs", New JArray(avs_array))

            scan_init_response = cyutils.call_with_json_input("scan/init", scan_item_json)
            If scan_init_response.SelectToken("success").ToObject(Of Boolean)() = False Then
                Console.Write(cyutils.get_error(scan_init_response))
                Console.ReadLine()
                Return
            End If
        Else
            'nitialize scan
            Console.Write("Please enter the path of file to scan: ")
            file_path = Console.ReadLine()
            Dim file_name As String = Path.GetFileName(file_path)

            Console.Write("Enter the name of the selected antivirus (with spaces): ")
            Dim avs As String = Console.ReadLine()

            Dim params2 As JObject = New JObject()
            params2.Add("file", file_name)
            params2.Add("token", apikey)
            params2.Add("avs", avs)
            scan_init_response = cyutils.call_with_form_input("scan/multiscanner/init", params2, "file", file_path)
            If scan_init_response.SelectToken("success").ToObject(Of Boolean)() = False Then
                Console.Write(cyutils.get_error(scan_init_response))
                Console.ReadLine()
                Return
            End If
        End If


        Dim guid As String = scan_init_response("guid").ToString()
        If scan_init_response("success").ToObject(Of Boolean) Then
            'et scan response
            Dim password_protected As Integer = CInt(scan_init_response("password_protected").Count())
            'heck if password-protected
            If password_protected > 0 Then
                Dim password_item_json As New JObject()
                For i As Integer = 0 To password_protected - 1
                    Dim password_file As String
                    Console.Write($"|Enter the Password file -> {scan_init_response("password_protected")(i)} |: ")
                    password_file = Console.ReadLine()
                    password_item_json("password") = password_file
                    password_item_json("token") = apikey
                    password_item_json("path") = scan_init_response("password_protected")(i)
                    cyutils.call_with_json_input($"scan/extract/{guid}", password_item_json)
                Next
            End If
        End If

        ' Start scan
        Console.WriteLine("=========  Start Scan ===========")
        Dim scan_json As New JObject()
        scan_json("token") = apikey
        Dim scan_response As JObject = cyutils.call_with_json_input($"scan/start/{guid}", scan_json)
        If scan_response("success").ToObject(Of Boolean) Then
            Dim is_finished As Boolean = False
            While Not is_finished
                Console.WriteLine("Waiting for result...")
                Dim input_json As New JObject()
                input_json("token") = apikey
                Dim scan_result_response As JObject = cyutils.call_with_json_input($"scan/result/{guid}", input_json)
                Try
                    If scan_result_response("data")("finished_at").Value(Of Integer) <> 0 Then
                        is_finished = True
                        Console.WriteLine(scan_result_response("data"))
                    End If
                Catch e As Exception
                    System.Threading.Thread.Sleep(5000)
                    Continue While
                End Try
            End While
        Else
            Console.WriteLine(cyutils.get_error(scan_response))
        End If
        Return
    End Sub

End Module