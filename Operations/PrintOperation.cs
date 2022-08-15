using System;
using System.Linq;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    public class PrintOperation : BaseOperation
    {
        public override bool PrintResultMessage => false;
        public static new bool CommandMatch(string line) => line.StartsWith("print", StringComparison.InvariantCultureIgnoreCase);

        //PRINT:RED Test
        //PRINT:BLACK|RED Test
        public PrintOperation(CopyCatScript script, string line) : base(script, line)
        {
        }

        public override ResultCode Execute()
        {
            string command = Line.Split(' ').First();
            string content = Line.Substring(Line.IndexOf(" ") + 1);

            var colorFromCommand = ParseCommandColor(command.Replace("println", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace("print", string.Empty, StringComparison.InvariantCultureIgnoreCase));

            Console.ResetColor();
            if (colorFromCommand.ForeColor.HasValue)
            {
                Console.ForegroundColor = colorFromCommand.ForeColor.Value;
            }
            if(colorFromCommand.BackColor.HasValue)
            {
                Console.BackgroundColor = colorFromCommand.BackColor.Value;
            }

            if(Line.StartsWith("println", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine(content);
            }
            else
            {
                Console.Write(content);
            }
            Console.ResetColor();
            return ResultCode.SUCCESS;
            
        }

        private (ConsoleColor? BackColor, ConsoleColor? ForeColor) ParseCommandColor(string line)
        {
            if(line.Contains(":"))
            {
                if(line.Contains("|"))
                {
                    string[] segments = line.Split('|');
                    Enum.TryParse<ConsoleColor>(segments[0], out var backColor);
                    Enum.TryParse<ConsoleColor>(segments[1], out var foreColor);
                    return (backColor, foreColor);
                }
                else
                {
                    Enum.TryParse<ConsoleColor>(line.Trim(':'), true, out var foreColor);
                    return (null, foreColor);
                }
            }
            return (null, null);
        }
    }
}
