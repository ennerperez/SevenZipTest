Imports SevenZip
Imports System.IO
Imports System.Threading
Imports System.Security.Cryptography
Imports System.Text

Module Program

    Sub Main()

        Dim args = Environment.GetCommandLineArgs()

        SevenZipCompressor.SetLibraryPath(Path.Combine(IIf(IntPtr.Size = 4, "x86", "x64"), "7z.dll"))
        Dim compressor = New SevenZipCompressor
        Dim target = args(1)
        Dim chunkSize As Integer = 0
        If args.Count() = 4 Then Integer.TryParse(args(3), chunkSize)

#If DEBUG Then
        If Not File.Exists(target) Then
#End If
            compressor.CompressFiles(target, args(2))
#If DEBUG Then
        End If
#End If

        Dim sha1 As New List(Of String)

        If chunkSize > 0 AndAlso File.Exists(target) Then
            sha1.AddRange(SplitFile(target, chunkSize))
        Else
            sha1.Add(GetHash(File.ReadAllBytes(target)))
        End If

        File.WriteAllText(target & ".sha", String.Join(Environment.NewLine, sha1))


    End Sub

    Public Function SplitFile(inputFile As String, chunkSize As Integer) As IEnumerable(Of String)
        Dim result As New List(Of String)


        Dim BUFFER_SIZE = 20 * 1024
        Dim buffer As Byte() = New Byte(BUFFER_SIZE) {}

        Using input = File.OpenRead(inputFile)
            Dim parts = CInt(Math.Ceiling(input.Length / chunkSize))
            Dim index = 0
            While input.Position < input.Length
                Dim filename = inputFile & String.Format(".{0:D" + IIf(parts.ToString().Length < 3, 3, parts.ToString().Length).ToString + "}", (index + 1))
                Using output = File.Create(filename)
                    Dim remaining = chunkSize
                    Dim bytesRead As Integer = 0
                    While remaining > 0 AndAlso bytesRead >= 0
                        bytesRead = input.Read(buffer, 0, Math.Min(remaining, BUFFER_SIZE))
                        If bytesRead = 0 Then Exit While
                        output.Write(buffer, 0, bytesRead)
                        remaining -= bytesRead
                    End While
                End Using
                result.Add(GetHash(File.ReadAllBytes(filename)))
                index += 1
                Thread.Sleep(500) 'experimental perhaps Try it
            End While
        End Using

#If Not DEBUG Then
        File.Delete(inputFile)
#End If

        Return result.AsEnumerable()
    End Function

    Public Function GetHash(buffer As Byte()) As String
        Using sha1 = New SHA1Managed()
            Dim hash = sha1.ComputeHash(buffer)
            Dim formatted = New StringBuilder(2 * hash.Length)
            For Each b As Byte In hash
                formatted.AppendFormat("{0:X2}", b)
            Next
            Return formatted.ToString()
        End Using
    End Function


End Module
