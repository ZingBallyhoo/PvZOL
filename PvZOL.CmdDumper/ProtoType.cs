using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace PvZOL.CmdDumper
{
    public record ProtoType(string m_typeName)
    {
        public List<ProtoField> m_fields = new List<ProtoField>();
        
        public void DecompileFields(string source)
        {
            var reader = new StringReader(source);
            
            while (reader.ReadLine() is { } line)
            {
                if (!line.Contains("public static const ") || !line.Contains("FieldDescriptor"))
                {
                    // not a field def
                    continue;
                }

                var fieldRegex = new Regex(@"public static const [^ ]+ = new (Repeated)?FieldDescriptor\$TYPE_([^ (]+)\((.+)\);$");
                var fieldMatch = fieldRegex.Match(line);
                if (!fieldMatch.Success)
                {
                    throw new InvalidDataException($"unable to match line: \"{line}\"");
                }

                var repeatedSpecifier = fieldMatch.Groups[1].Value;
                var dataType = fieldMatch.Groups[2].Value;
                var ctorParams = fieldMatch.Groups[3].Value;

                var ctorParamsList = ctorParams.Split(',');
                var ctorParam_FullName = ctorParamsList[0];
                var ctorParam_ShortName = ctorParamsList[1];
                var ctorParam_Flags = ctorParamsList[2];
                //var ctorParam_Dto = ctorParamsList[3]; // if present

                var fieldNumRegex = new Regex(@"(\d+) << 3");
                var fieldNumMatch = fieldNumRegex.Match(ctorParam_Flags);
                if (!fieldNumMatch.Success)
                {
                    throw new InvalidDataException($"unable to match field number: {ctorParam_Flags}");
                }
    
                //Console.Out.WriteLine($"{dataType} {ctorParams}");

                var csType = dataType switch
                {
                    "BOOL" => "bool",
                    "INT32" => "int",
                    "UINT32" => "uint",
                    "INT64" => "long",
                    "UINT64" => "ulong",
                    "FLOAT" => "float",
                    "DOUBLE" => "double",
                    "STRING" => "string",
                    "BYTES" => "byte[]",
                    "MESSAGE" => ctorParamsList[3],
                    "ENUM" => ctorParamsList[3],
                    _ => throw new NotImplementedException($"unknown as type: {dataType}")
                };

                var field = new ProtoField(
                    ctorParam_ShortName.Trim('"'),
                    int.Parse(fieldNumMatch.Groups[1].ValueSpan),
                    csType,
                    repeatedSpecifier.Length != 0);
                m_fields.Add(field);
                
                Console.Out.WriteLine(field);
            }
        }

        public string Emit()
        {
            var writer = new IndentedTextWriter(new StringWriter());
            writer.WriteLine("namespace PvZOL.Cmd.Gen.Types;");
            
            writer.WriteLine("[ProtoContract]");
            writer.WriteLine($"public class {m_typeName}");
            writer.WriteLine("{");
            writer.Indent++;

            foreach (var field in m_fields)
            {
                var type = field.m_elementType;
                if (field.m_isRepeated)
                {
                    type = $"List<{type}>";
                }
                
                writer.Write($"[ProtoMember({field.m_number})] public {type} m_{field.m_name}");
                if (field.m_isRepeated)
                {
                    writer.Write(" = new()");
                }
                
                writer.WriteLine(";");
            }

            writer.Indent--;
            writer.WriteLine("}");

            return writer.InnerWriter.ToString()!;
        }
    }

    public record ProtoField(string m_name, int m_number, string m_elementType, bool m_isRepeated);
}