// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RazorSyntaxGenerator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 2)
            {
                WriteUsage();
                return 1;
            }

            var inputFile = args[0];

            if (!File.Exists(inputFile))
            {
                Console.WriteLine(Directory.GetCurrentDirectory());
                Console.WriteLine(inputFile + " not found.");
                return 1;
            }

            var writeSource = true;
            var writeSignatures = false;
            string outputFile = null;

            if (args.Length == 2)
            {
                if (args[1] == "/sig")
                {
                    writeSignatures = true;
                }
                else
                {
                    outputFile = args[1];
                }
            }

            var reader = XmlReader.Create(inputFile, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var serializer = new XmlSerializer(typeof(Tree));
            var tree = (Tree)serializer.Deserialize(reader);

            if (writeSignatures)
            {
                SignatureWriter.Write(Console.Out, tree);
            }
            else
            {
                if (writeSource)
                {
                    var outputPath = outputFile.Trim('"');
                    var prefix = Path.GetFileName(inputFile);
                    var outputMainFile = Path.Combine(outputPath, $"{prefix}.Main.Generated.cs");
                    var outputInternalFile = Path.Combine(outputPath, $"{prefix}.Internal.Generated.cs");
                    var outputSyntaxFile = Path.Combine(outputPath, $"{prefix}.Syntax.Generated.cs");

                    WriteToFile(tree, SourceWriter.WriteMain, outputMainFile);
                    WriteToFile(tree, SourceWriter.WriteInternal, outputInternalFile);
                    WriteToFile(tree, SourceWriter.WriteSyntax, outputSyntaxFile);
                }
                //if (writeTests)
                //{
                //    WriteToFile(tree, TestWriter.Write, outputFile);
                //}
            }

            return 0;
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Invalid usage");
            Console.WriteLine(typeof(Program).GetTypeInfo().Assembly.ManifestModule.Name + " input-file output-file [/write-test]");
        }

        private static void WriteToFile(Tree tree, Action<TextWriter, Tree> writeAction, string outputFile)
        {
            var stringBuilder = new StringBuilder();
            var writer = new StringWriter(stringBuilder);
            writeAction(writer, tree);

            var text = stringBuilder.ToString();
            int length;
            do
            {
                length = text.Length;
                text = text.Replace("{\r\n\r\n", "{\r\n");
            } while (text.Length != length);

            try
            {
                using (var outFile = new StreamWriter(File.Open(outputFile, FileMode.Create), Encoding.UTF8))
                {
                    outFile.Write(text);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Unable to access {0}.  Is it checked out?", outputFile);
            }
        }
    }
}
