using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace PvZOL.Tools.CmdDumper
{
    public record ProtoEnum
    {
        public readonly string m_typeName;
        public readonly List<ProtoEnumValue> m_values = new List<ProtoEnumValue>();

        public ProtoEnum(string typeName)
        {
            m_typeName = typeName switch
            {
                "ErrorDefineType" => "Error",
                _ => typeName
            };
        }

        public void DecompileFields(string source)
        {
            var reader = new StringReader(source);

            while (reader.ReadLine() is { } line)
            {
                if (!line.Contains("public static const "))
                {
                    // not an enum value
                    continue;
                }

                var enumValueRegex = new Regex("public static const ([^ ]+):int = (-?\\d+);$");
                var enumValueMatch = enumValueRegex.Match(line);
                if (!enumValueMatch.Success)
                {
                    throw new InvalidDataException($"unable to match line: \"{line}\"");
                }
                
                //Console.Out.WriteLine(line);

                var valueName = enumValueMatch.Groups[1].Value;
                var valueValue = int.Parse(enumValueMatch.Groups[2].Value);
                
                // assumption is false
                // E_AasAdultType::AasType_Adult
                /*var prefix = $"{m_typeName}_";
                if (!valueName.StartsWith(prefix))
                {
                    throw new InvalidDataException($"enum value should be prefixed with type name: \"{line}\"");
                }
                valueName = valueName.Substring(prefix.Length);*/
                
                valueName = string.Join('_', valueName.Split('_').Skip(1));
                if (char.IsNumber(valueName[0]))
                {
                    valueName = $"_{valueValue}";
                }

                valueName = char.ToUpperInvariant(valueName[0]) + valueName[1..];
                
                //Console.Out.WriteLine($"{valueName} = {valueValue}");
                m_values.Add(new ProtoEnumValue(valueName, valueValue));
            }
        }
    
        public string Emit()
        {
            var writer = new IndentedTextWriter(new StringWriter());
            writer.WriteLine("namespace PvZOL.Protocol.Cmd.Enums;");
            writer.WriteLine();
            
            writer.WriteLine($"public enum {m_typeName}");
            writer.WriteLine("{");
            writer.Indent++;

            foreach (var value in m_values)
            {
                writer.WriteLine($"{value.m_name} = {value.m_value},");
            }

            writer.Indent--;
            writer.WriteLine("}");

            return writer.InnerWriter.ToString()!;
        }
    }

    public record ProtoEnumValue(string m_name, int m_value);
}