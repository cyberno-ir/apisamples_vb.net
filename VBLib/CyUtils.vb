Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports Newtonsoft.Json.Linq

Public Class CyUtils
    Private Const USER_AGENT = "Cyberno-API-Sample-VBNet"

    Private server_address As String
    Dim unknownerror_respone_json As JObject

    Public Sub New(ByVal server_address As String)
        Me.server_address = server_address

        unknownerror_respone_json = New JObject
        unknownerror_respone_json.Add("error_code", 900)
        unknownerror_respone_json.Add("success", False)
    End Sub

    Public Function get_sha256(file_path As String) As String
        Using sha256_var = SHA256.Create()
            Using stream = File.OpenRead(file_path)
                Dim file_sha256() As Byte = sha256_var.ComputeHash(stream)
                Dim Hex As StringBuilder = New StringBuilder(file_sha256.Length * 2)
                For Each b As Byte In file_sha256
                    Hex.AppendFormat("{0:x2}", b)
                Next
                Return Hex.ToString()
            End Using
        End Using
    End Function

    Public Function get_error(return_value As JObject)
        Dim error_list As New System.Text.StringBuilder()
        error_list.Append("Error!" + vbCrLf)
        If return_value.ContainsKey("error_code") = True Then
            error_list.Append("Error code: " + return_value.Item("error_code").ToString + vbCrLf)
        End If
        If return_value.ContainsKey("error_desc") = True Then
            error_list.Append("Error description: " + return_value.Item("error_desc").ToString + vbCrLf)
        End If
        Return error_list
    End Function

    Public Function call_with_json_input(ByVal api As String, ByVal json_input As JObject) As JObject
        Dim HttpWebRequest As HttpWebRequest = WebRequest.Create(Me.server_address + api)
        HttpWebRequest.ContentType = "application/json"
        HttpWebRequest.Method = "POST"
        HttpWebRequest.UserAgent = USER_AGENT
        Try
            Using streamWriter = New StreamWriter(HttpWebRequest.GetRequestStream())
                Dim parsedContent As String = json_input.ToString()
                streamWriter.Write(parsedContent)
                streamWriter.Flush()
                streamWriter.Close()
            End Using
        Catch
            Return unknownerror_respone_json
        End Try
        Dim result As String
        Try
            Dim httpResponse As HttpWebResponse = HttpWebRequest.GetResponse()
            Using streamReader = New StreamReader(httpResponse.GetResponseStream())
                result = streamReader.ReadToEnd()
            End Using
        Catch ex As WebException
            If ex.Response Is Nothing Then
                Return unknownerror_respone_json
            End If
            Using stream = ex.Response.GetResponseStream()
                Using reader = New StreamReader(stream)
                    result = reader.ReadToEnd()
                End Using
            End Using
        End Try
        Try
            Dim srtr As JObject
            srtr = JObject.Parse(result)
            Return srtr
        Catch ex As Exception
            Return unknownerror_respone_json
        End Try
    End Function

    Public Function call_with_form_input(ByVal api As String, ByVal data_input As JObject, ByVal file_param_name As String, ByVal file_path As String) As JObject
        Dim boundary As String = "---------------------------" + DateTime.Now.Ticks.ToString("x")
        Dim boundarybytes = System.Text.Encoding.ASCII.GetBytes(vbCrLf + "--" + boundary + vbCrLf)
        Dim wr As HttpWebRequest = WebRequest.Create(Me.server_address + api)
        wr.ContentType = "multipart/form-data; boundary=" + boundary
        wr.Method = "POST"
        wr.KeepAlive = True
        wr.Headers.Add("UserAgent", USER_AGENT)
        wr.Credentials = System.Net.CredentialCache.DefaultCredentials
        Dim rs = wr.GetRequestStream()
        Dim formdataTemplate = "Content-Disposition: form-data; name=""{0}""" + vbCrLf + vbCrLf + "{1}"
        Dim data_input_mover = data_input.First
        For i As Integer = 0 To data_input.Count - 1
            rs.Write(boundarybytes, 0, boundarybytes.Length)
            Dim key As String = CType(data_input_mover, JProperty).Name
            Dim value = data_input_mover.First.Value(Of String)
            Dim formitem = String.Format(formdataTemplate, key, value)
            Dim formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem)
            rs.Write(formitembytes, 0, formitembytes.Length)
            data_input_mover = data_input_mover.Next
        Next
        rs.Write(boundarybytes, 0, boundarybytes.Length)
        Dim headerTemplate = "Content-Disposition: form-data; name=""{0}""; filename=""{1}""" + vbCrLf + vbCrLf
        Dim header = String.Format(headerTemplate, file_param_name, "file")
        Dim headerbytes = System.Text.Encoding.UTF8.GetBytes(header)
        rs.Write(headerbytes, 0, headerbytes.Length)
        Dim FileStream = New FileStream(file_path, FileMode.Open, FileAccess.Read)
        Dim buffer(4096) As Byte
        Dim bytesRead = 0
        Do
            bytesRead = FileStream.Read(buffer, 0, buffer.Length)
            If (bytesRead <> 0) Then rs.Write(buffer, 0, bytesRead)
        Loop Until bytesRead = 0
        FileStream.Close()

        Dim trailer = System.Text.Encoding.ASCII.GetBytes(vbCrLf + "--" + boundary + "--" + vbCrLf)
        rs.Write(trailer, 0, trailer.Length)
        rs.Close()

        Dim result As String
        Try
            Dim httpResponse As HttpWebResponse = wr.GetResponse()
            Using streamReader = New StreamReader(httpResponse.GetResponseStream())
                result = streamReader.ReadToEnd()
            End Using
        Catch ex As WebException
            If ex.Response Is Nothing Then
                Return unknownerror_respone_json
            End If
            Using stream = ex.Response.GetResponseStream()
                Using reader = New StreamReader(stream)
                    result = reader.ReadToEnd()
                End Using
            End Using
        End Try
        Try
            Dim srtr As JObject
            srtr = JObject.Parse(result)
            Return srtr
        Catch ex As Exception
            Return unknownerror_respone_json
        End Try
    End Function

End Class