using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace blac
{
  class Program
  {
    static bool trace = true;

    static int bracketCounter = 0;

    public static void Error(string msg, int line)
    {

      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine("ERROR: " + msg + $" (Line {line})");
      Console.ResetColor();

      if (trace)
        System.Diagnostics.Debug.Assert(false);
      else
        Environment.Exit(-1);
    }

    public static void Warning(string msg, int line)
    {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine("WARNING: " + msg + $" (Line {line})");
      Console.ResetColor();
    }

    public static void What(string msg, int line)
    {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.BackgroundColor = ConsoleColor.Magenta;
      Console.WriteLine("Oh... " + msg + $" Risky. I like that. (Line {line})");
      Console.ResetColor();
    }


    static void Main(string[] args)
    {
      if(args.Length != 1)
      {
        Console.WriteLine("BLAH.\n\nUsage: blahng <file>\n");
        Environment.Exit(1);
        return;
      }

      string[] file = System.IO.File.ReadAllLines(args[0]);
      Dictionary<ulong, Function> functions = new Dictionary<ulong, Function>();
      Function currentFunction = null;

      for (int i = 0; i < file.Length; i++)
      {
        string s = file[i].TrimStart(' ', '\t');

        if (s.StartsWith("//"))
          continue;

        if(currentFunction != null)
        {
          if(bracketCounter == 0)
          {
            if (s.Length > 0 && s[0] == '{')
              bracketCounter++;
          }
          else
          {
            if (s.Length > 0 && s[0] == '}')
            {
              bracketCounter--;

              if(bracketCounter == 0)
              {
                currentFunction = null;
              }
            }
            else
            {
              currentFunction.ParseAddToTree(s, i);
            }
          }
        }
        else // if !inFunction
        {
          if (s.Length == 0)
            continue;

          int length = -1;

          for (int j = 0; j < s.Length; j++)
          {
            if (s[j] == '(')
            {
              length = j;
              break;
            }
          }

          if (length == -1)
            continue;

          string num = s.Substring(0, length);

          ulong funcNum;

          if (!ulong.TryParse(num, out funcNum))
            Error($"Invalid function number '{num}'.", i + 1);

          s = s.Substring(length + 1);
          length = -1;

          for (int j = 0; j < s.Length; j++)
          {
            if (s[j] == ')')
            {
              length = j;
              break;
            }
          }

          if(length == -1)
            Error($"Missing closing bracket.", i + 1);

          if (functions.ContainsKey(funcNum))
          {
            What($"Youre using function index {funcNum} multiple times. nice.", i + 1);
            currentFunction = functions[funcNum];
          }
          else
          {
            currentFunction = new Function();
            functions.Add(funcNum, currentFunction);
          }

          bracketCounter = 0;
        }
      }

      foreach (var func in functions)
      {
        if (trace)
          Console.WriteLine("Compiling function " + func.Key);

        func.Value.GenerateByteCode();

        if (trace)
        {
          foreach (var c in func.Value.commands)
            Console.WriteLine(c);

          Console.ReadLine();
        }
      }

      if (trace)
        Console.ReadLine();
    }
  }

  struct Command
  {
    public ECommand command;
    public EDataType type0, type1;
    public Varbit varbit0, varbit1;
    public uint param0, param1;
    public long number0;
    public byte threebit0;

    public Command(ECommand cmd, uint param0) : this()
    {
      command = cmd;
      this.param0 = param0;
    }

    public Command(ECommand cmd, byte threebit0) : this()
    {
      command = cmd;
      this.threebit0 = threebit0;
    }

    public Command(ECommand cmd, uint param0, uint param1) : this(cmd, param0)
    {
      this.param1 = param1;
    }

    public Command(ECommand cmd, uint param0, byte threebit0) : this(cmd, param0)
    {
      command = cmd;
      this.threebit0 = threebit0;
    }

    public Command(ECommand cmd, uint param0, Varbit varbit0) : this(cmd, param0)
    {
      this.varbit0 = varbit0;
    }

    public Command(ECommand cmd, uint param0, uint param1, EDataType type) : this(cmd, param0, param1)
    {
      this.type0 = type;
    }

    public Command(ECommand cmd, uint param0, uint param1, EDataType type0, EDataType type1) : this(cmd, param0, param1, type0)
    {
      this.type1 = type1;
    }

    public Command(ECommand cmd, uint param0, long number0) : this(cmd, param0)
    {
      this.number0 = number0;
    }

    public List<byte> Serialize()
    {
      List<byte> ret = new List<byte>();
      ret.Add((byte)command);

      switch(command)
      {
        case ECommand.STACKALLOC:
          ret.AddRange(BitConverter.GetBytes(param0));
          break;

        case ECommand.ASSIGN_VARBIT:
        case ECommand.get:
        case ECommand.pull:
        case ECommand.leaf:
          ret.AddRange(BitConverter.GetBytes(param0));
          ret.AddRange(BitConverter.GetBytes(varbit0.length));
          ret.AddRange(BitConverter.GetBytes(varbit0.bytes.Count));

          for (int i = 0; i < varbit0.bytes.Count; i++)
            ret.Add(varbit0.bytes[i]);

          break;

        case ECommand.COPY:
          ret.AddRange(BitConverter.GetBytes(param0));
          ret.AddRange(BitConverter.GetBytes(param1));
          break;

        case ECommand.split:
        case ECommand.close:
          ret.AddRange(BitConverter.GetBytes(param0));
          break;

        case ECommand.move:
        case ECommand.set:
        case ECommand.push:
          ret.AddRange(BitConverter.GetBytes(param0));
          ret.AddRange(BitConverter.GetBytes(param1));
          ret.AddRange(BitConverter.GetBytes(varbit0.length));
          ret.AddRange(BitConverter.GetBytes(varbit0.bytes.Count));

          for (int i = 0; i < varbit0.bytes.Count; i++)
            ret.Add(varbit0.bytes[i]);
          break;

        case ECommand.cast:
          ret.AddRange(BitConverter.GetBytes(param0));
          ret.AddRange(BitConverter.GetBytes(param1));
          ret.Add((byte)type0);
          ret.Add((byte)type1);
          break;

        case ECommand.ADD:
        case ECommand.SUBTRACT:
        case ECommand.MULTIPLY:
        case ECommand.DIVIDE:
        case ECommand.MODULO:
        case ECommand.AND:
        case ECommand.OR:
        case ECommand.XOR:
          ret.AddRange(BitConverter.GetBytes(param0));
          ret.AddRange(BitConverter.GetBytes(param1));
          ret.Add((byte)type0);
          break;
      }

      return ret;
    }

    public override string ToString()
    {
      return $"[{command}] param0: {param0}, param1: {param1}, varbit0: " + (varbit0 == null ? "<null>" : $"[{varbit0.length}] '{varbit0.bytes.ToVarbitNum()}' ({varbit0.bytes.ToVarbitString()})") + ", varbit1: " + (varbit1 == null ? "<null>" : $"[{varbit1.length}] '{varbit1.bytes.ToVarbitNum()}' ({varbit1.bytes.ToVarbitString()})") + $", number0: {number0}, threebit0: {threebit0}, dataType0: {type0}, dataType1: {type1}";
    }
  }

  enum ECommand : byte
  {
    ____NOP____,
    set,
    get,
    exec,
    split,
    leaf,
    close,
    move,

    IF,
    IFNAN,
    GOTO,

    ASSIGN_VARBIT,
    ASSIGN_3BIT,
    ASSIGN_NUM,

    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,
    INCREMENT,

    __PREDEFINE_LABEL,
    __SET_LABEL,

    IS_NAN,
    XOR,
    BITSHIFTL,
    BITSHIFTR,

    COPY,
    
    STACKALLOC,
    INLINE_EXEC,
    pull,
    push,
    cast,
    AND,
    OR,
    MODULO,
  }

  enum EDataType
  {
    _varbit,
    _3bit,
    _num
  }

  enum ESyntaxItemType
  {
    LINE_END,
    ASSIGN,
    AB_OPERATOR,
    IF,
    IFNOT,
    BRACKET,
    COMMA,
    NAME,
    TYPE,
    NOP,
    GOTO,
    FUNCTION,
    VALUE,
    CAST,
    LABEL,
    HERE
  }

  class SyntaxItem
  {
    public List<SyntaxItem> subItems = new List<SyntaxItem>();
    public int line;
    public string data;
    public ESyntaxItemType itemType;
    public SyntaxItem parent = null;
    public int parentPosition;
    public Function function;

    public SyntaxItem(string data, SyntaxItem parent, int line, Function function)
    {
      if(data != null)
        data = data.Trim();

      this.line = line;
      this.parent = parent;
      this.data = data;
      this.function = function;

      if (parent != null)
      {
        parentPosition = parent.subItems.Count;
        parent.subItems.Add(this);
      }

      if (string.IsNullOrWhiteSpace(data))
      {
        itemType = ESyntaxItemType.NOP;
      }
      else if (new string[] { "varbit", "3bit", "num" }.Contains(data))
      {
        itemType = ESyntaxItemType.TYPE;
      }
      else if (new string[] { "here" }.Contains(data))
      {
        itemType = ESyntaxItemType.HERE;
      }
      else if (new string[] { "+", "-", "*", "/", "%", "&", "|", "^" }.Contains(data))
      {
        itemType = ESyntaxItemType.AB_OPERATOR;
      }
      else if ("=" == (data))
      {
        itemType = ESyntaxItemType.ASSIGN;
      }
      else if ("," == (data))
      {
        itemType = ESyntaxItemType.COMMA;
      }
      else if (new string[] { "(", ")", "[", "]" }.Contains(data))
      {
        itemType = ESyntaxItemType.BRACKET;
      }
      else if (";" == (data))
      {
        itemType = ESyntaxItemType.LINE_END;
      }
      else if ("if" == data)
      {
        itemType = ESyntaxItemType.IF;
      }
      else if ("ifn" == (data))
      {
        itemType = ESyntaxItemType.IFNOT;
      }
      else if ("goto" == (data))
      {
        itemType = ESyntaxItemType.GOTO;
      }
      else if ("cast" == (data))
      {
        itemType = ESyntaxItemType.CAST;
      }
      else if (new string[] { "set", "get", "split", "close", "isleaf", "exec", "move", "pull", "push" }.Contains(data))
      {
        itemType = ESyntaxItemType.FUNCTION;
      }
      else if(char.IsDigit(data[0]))
      {
        itemType = ESyntaxItemType.VALUE;
      }
      else
      {
        itemType = ESyntaxItemType.NAME;
      }
    }

    public void Cleanup()
    {
      for (int i = subItems.Count - 1; i >= 0; i--)
        if (subItems[i].itemType == ESyntaxItemType.NOP)
          subItems.RemoveAt(i);

      foreach (var i in subItems)
        i.Cleanup();
    }

    public SyntaxItem GetHighest()
    {
      if (parent == null)
        return this;
      else
        return parent.GetHighest();
    }
  }

  class Variable
  {
    public string name;
    public EDataType type;
    public readonly uint addr;

    public Variable(string name, string type, Context context)
    {
      this.name = name;

      foreach (var x in Enum.GetNames(typeof(EDataType)))
        if (x.EndsWith(type))
          this.type = (EDataType)Enum.Parse(typeof(EDataType), x);

      addr = context.nextAddress++;
    }
  }

  class Label
  {
    public string name;
    public int? position;
    public int predefinedLine;
    public int definedLine;
    public uint index;
  }

  class Context
  {
    public List<Variable> declaredVars = new List<Variable>();
    public List<Label> declaredLabels = new List<Label>();
    internal uint nextAddress = 0;
  }

  class Function
  {
    public List<Command> commands = new List<Command>();
    SyntaxItem currentItem;

    public uint StackSize = 0;

    public Function()
    {
      currentItem = new SyntaxItem(null, null, -1, this);
    }

    internal void GenerateByteCode()
    {
      this.currentItem = currentItem.GetHighest();
      this.currentItem.Cleanup();

      Context context = new Context();
      this.commands = GetCommands(context, currentItem.subItems, 0);
      this.StackSize = context.nextAddress;
      commands.Insert(0, new Command(ECommand.STACKALLOC, StackSize));
    }

    internal void ParseAddToTree(string s, int line)
    {
      var data = s.SplitKeep(new string[] { ";", " ", ",", "++", "--", "#", "&", "@", "+", "-", "*", "/", "->", "<-", ":", "(", ")", "{", "}", "[", "]", "%", "$", "!", "^", "~", "==", "=", "|", "<<", ">>", "<", ">", "<=", ">=" });

      foreach (var item in data)
        new SyntaxItem(item, currentItem, line, this);
    }

    static string[] returningFunctions = new string[] { "get", "isleaf", "pull" };
    static string[] oneParamNoRetFunctions = new string[] { "split", "close" };
    static string[] twoParamNoRetFunctions = new string[] { "move", "exec", "set", "push" };

    internal List<Command> GetCommands(Context currentContext, List<SyntaxItem> items, int pos)
    {
      List<Command> ret = new List<Command>();
      Variable selectedVariable = null;

      while (pos < items.Count)
      {
        if (items.Count > pos + 1 && items[pos].itemType == ESyntaxItemType.TYPE && items[pos + 1].itemType == ESyntaxItemType.NAME) // var declaration
        {
          if ((from s in currentContext.declaredVars where s.name == items[pos + 1].data select 0).Any())
            Program.Error($"Variable '{items[pos + 1].data}' redefinition.", items[pos + 1].line);

          currentContext.declaredVars.Add(new Variable(items[pos + 1].data, items[pos].data, currentContext));

          if (items[pos + 2].itemType == ESyntaxItemType.LINE_END)
            pos += 3;
          else if (items[pos + 2].itemType == ESyntaxItemType.ASSIGN)
          {
            selectedVariable = (from v in currentContext.declaredVars where v.name == items[pos + 1].data select v).First();
            pos += 3;
          }
          else
            Program.Error($"Unexpected statement '{items[pos + 2].data}'.", items[pos + 2].line);
        }
        else if (items.Count > pos + 3 && items[pos].itemType == ESyntaxItemType.FUNCTION && items[pos + 1].itemType == ESyntaxItemType.BRACKET && (items[pos + 2].itemType == ESyntaxItemType.NAME || items[pos + 2].itemType == ESyntaxItemType.VALUE) && items[pos + 3].itemType == ESyntaxItemType.BRACKET)
        {
          if (returningFunctions.Contains(items[pos].data))
          {
            if (selectedVariable.type != EDataType._num && (items[pos].data == "isleaf"))
              Program.Error($"Builtin Function '{items[pos].data}' is assigned to a variable of type '{selectedVariable.type}' but returns '_num'.", items[pos].line);

            if (selectedVariable.type != EDataType._varbit && (items[pos].data == "pull" || items[pos].data == "get"))
              Program.Error($"Builtin Function '{items[pos].data}' is assigned to a variable of type '{selectedVariable.type}' but returns '_varbit'.", items[pos].line);

            uint addr = 0;

            if (items[pos + 2].itemType == ESyntaxItemType.VALUE)
            {
              addr = currentContext.nextAddress++;

              ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr, DecodeVarbitValue(items[pos + 2].data, items[pos + 2].line)));
            }
            else
            {
              var x = (from v in currentContext.declaredVars where v.name == items[pos + 2].data select v);

              if (!x.Any())
                Program.Error($"Expected variable name or value but got '{items[pos + 2].data}'.", items[pos + 2].line);

              if (x.First().type != EDataType._varbit)
                Program.Error($"Variable '{items[pos + 2].data}' that is passed to function '{items[pos].data}' is of type '{x.First().type}' but should be '_varbit'.", items[pos + 2].line);

              addr = x.First().addr;
            }

            switch (items[pos].data)
            {
              case "get":
                ret.Add(new Command(ECommand.get, addr, selectedVariable.addr));
                break;

              case "isleaf":
                ret.Add(new Command(ECommand.leaf, addr, selectedVariable.addr));
                break;

              case "pull":
                ret.Add(new Command(ECommand.pull, addr, selectedVariable.addr));
                break;
            }

            pos += 4;
          }
          else if (selectedVariable != null)
          {
            Program.Error($"Builtin Function '{items[pos].data}' is assigned to variable '{selectedVariable.name}' but does not return a value.", items[pos].line);
          }
          else
          {
            if (oneParamNoRetFunctions.Contains(items[pos].data))
            {
              if (!(items.Count > pos + 3 && items[pos].itemType == ESyntaxItemType.FUNCTION && items[pos + 1].itemType == ESyntaxItemType.BRACKET && (items[pos + 2].itemType == ESyntaxItemType.NAME || items[pos + 2].itemType == ESyntaxItemType.VALUE) && items[pos + 3].itemType == ESyntaxItemType.BRACKET))
                Program.Error($"Function '{items[pos].data}' can only be called with one argument.", items[pos].line);

              uint addr = 0;

              if (items[pos + 2].itemType == ESyntaxItemType.VALUE)
              {
                addr = currentContext.nextAddress++;

                ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr, DecodeVarbitValue(items[pos + 2].data, items[pos + 2].line)));
              }
              else
              {
                var x = (from v in currentContext.declaredVars where v.name == items[pos + 2].data select v);

                if (!x.Any())
                  Program.Error($"Expected variable name or value but got '{items[pos + 2].data}'.", items[pos + 2].line);

                if (x.First().type != EDataType._varbit)
                  Program.Error($"Variable '{items[pos + 2].data}' that is passed to function '{items[pos].data}' is of type '{x.First().type}' but should be '_varbit'.", items[pos + 2].line);

                addr = x.First().addr;
              }

              switch (items[pos].data)
              {
                case "split":
                  ret.Add(new Command(ECommand.split, addr));
                  break;

                case "close":
                  ret.Add(new Command(ECommand.close, addr));
                  break;
              }

              pos += 4;
            }
            else if (twoParamNoRetFunctions.Contains(items[pos].data))
            {
              Program.Error($"Function '{items[pos].data}' can only be called with two arguments.", items[pos].line);
            }
            else
            {
              Program.Error($"Calling function '{items[pos].data}' with one argument not supported.", items[pos].line);
            }
          }
        }
        else if (items.Count > pos + 5 && items[pos].itemType == ESyntaxItemType.FUNCTION && items[pos + 1].itemType == ESyntaxItemType.BRACKET && (items[pos + 2].itemType == ESyntaxItemType.NAME || items[pos + 2].itemType == ESyntaxItemType.VALUE) && items[pos + 3].itemType == ESyntaxItemType.COMMA && (items[pos + 4].itemType == ESyntaxItemType.NAME || items[pos + 4].itemType == ESyntaxItemType.VALUE) && items[pos + 5].itemType == ESyntaxItemType.BRACKET)
        {
          if (oneParamNoRetFunctions.Contains(items[pos].data))
            Program.Error($"Function '{items[pos].data}' can only be called with two arguments.", items[pos].line);
          if (!twoParamNoRetFunctions.Contains(items[pos].data))
            Program.Error($"Calling function '{items[pos].data}' with two arguments not supported.", items[pos].line);

          uint addr0 = 0;

          if (items[pos + 2].itemType == ESyntaxItemType.VALUE)
          {
            addr0 = currentContext.nextAddress++;

            ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr0, DecodeVarbitValue(items[pos + 2].data, items[pos + 2].line)));
          }
          else
          {
            var x = (from v in currentContext.declaredVars where v.name == items[pos + 2].data select v);

            if (!x.Any())
              Program.Error($"Expected variable name or value but got '{items[pos + 2].data}'.", items[pos + 2].line);

            if (x.First().type != EDataType._varbit)
              Program.Error($"Variable '{items[pos + 2].data}' that is passed to function '{items[pos].data}' is of type '{x.First().type}' but should be '_varbit'.", items[pos + 2].line);

            addr0 = x.First().addr;
          }

          uint addr1 = 0;

          if (items[pos + 4].itemType == ESyntaxItemType.VALUE)
          {
            addr1 = currentContext.nextAddress++;

            ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr1, DecodeVarbitValue(items[pos + 4].data, items[pos + 4].line)));
          }
          else
          {
            var x = (from v in currentContext.declaredVars where v.name == items[pos + 4].data select v);

            if (!x.Any())
              Program.Error($"Expected variable name or value but got '{items[pos + 4].data}'.", items[pos + 4].line);

            if (x.First().type != EDataType._varbit)
              Program.Error($"Variable '{items[pos + 4].data}' that is passed to function '{items[pos].data}' is of type '{x.First().type}' but should be '_varbit'.", items[pos + 4].line);

            addr1 = x.First().addr;
          }


          switch (items[pos].data)
          {
            case "move":
              ret.Add(new Command(ECommand.move, addr0, addr1));
              break;

            case "set":
              ret.Add(new Command(ECommand.set, addr0, addr1));
              break;

            case "pull":
              ret.Add(new Command(ECommand.pull, addr0, addr1));
              break;
          }

          pos += 6;
        }
        else if (items.Count > pos + 3 && items[pos].itemType == ESyntaxItemType.CAST && items[pos + 1].itemType == ESyntaxItemType.BRACKET && (items[pos + 2].itemType == ESyntaxItemType.NAME || items[pos + 2].itemType == ESyntaxItemType.VALUE) && items[pos + 3].itemType == ESyntaxItemType.BRACKET)
        {
          if (selectedVariable == null)
          {
            Program.Error($"Builtin Function '{items[pos].data}' returns a value, but is not assigned to a variable.", items[pos].line);
          }
          else
          {
            uint addr = 0;
            EDataType type = EDataType._varbit;

            if (items[pos + 2].itemType == ESyntaxItemType.VALUE)
            {
              addr = currentContext.nextAddress++;

              ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr, DecodeVarbitValue(items[pos + 2].data, items[pos + 2].line)));
            }
            else
            {
              var x = (from v in currentContext.declaredVars where v.name == items[pos + 2].data select v);

              if (!x.Any())
                Program.Error($"Expected variable name or value but got '{items[pos + 2].data}'.", items[pos + 2].line);

              addr = x.First().addr;
              type = x.First().type;
            }

            ret.Add(new Command(ECommand.cast, addr, selectedVariable.addr, type, selectedVariable.type));
            pos += 4;
          }
        }
        else if (selectedVariable != null && items[pos].itemType == ESyntaxItemType.NAME)
        {
          var x = (from v in currentContext.declaredVars where v.name == items[pos].data select v);

          if (!x.Any())
            Program.Error($"Expected variable name or value but got '{items[pos].data}'.", items[pos].line);

          if (x.First().type != selectedVariable.type)
            Program.Error($"Variable '{items[pos + 2].data}' is of type '{x.First().type}' but should be '{selectedVariable.type}'.", items[pos + 2].line);

          ret.Add(new Command(ECommand.COPY, selectedVariable.addr, x.First().addr));

          pos++;
        }
        else if (selectedVariable != null && items[pos].itemType == ESyntaxItemType.VALUE)
        {
          if (selectedVariable.type == EDataType._varbit)
            ret.Add(new Command(ECommand.ASSIGN_VARBIT, selectedVariable.addr, DecodeVarbitValue(items[pos].data, items[pos].line)));
          else if (selectedVariable.type == EDataType._3bit)
            ret.Add(new Command(ECommand.ASSIGN_3BIT, selectedVariable.addr, Decode3BitValue(items[pos].data, items[pos].line)));
          else if (selectedVariable.type == EDataType._num)
            ret.Add(new Command(ECommand.ASSIGN_NUM, selectedVariable.addr, DecodeNumValue(items[pos].data, items[pos].line)));
          else
            Program.Error($"Unsupported type '{selectedVariable.type}' to assign value '{items[pos]}' to.", items[pos].line);
          pos++;
        }
        else if (items[pos].itemType == ESyntaxItemType.LINE_END)
        {
          selectedVariable = null;
          pos++;
        }
        else if (items.Count > pos + 1 && selectedVariable != null && items[pos].itemType == ESyntaxItemType.AB_OPERATOR && (items[pos + 1].itemType == ESyntaxItemType.NAME || items[pos + 1].itemType == ESyntaxItemType.VALUE))
        {
          uint addr = 0;

          if (items[pos + 1].itemType == ESyntaxItemType.VALUE)
          {
            addr = currentContext.nextAddress++;

            if (selectedVariable.type == EDataType._varbit)
              ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr, DecodeVarbitValue(items[pos + 1].data, items[pos + 1].line)));
            else if (selectedVariable.type == EDataType._3bit)
              ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr, Decode3BitValue(items[pos + 1].data, items[pos + 1].line)));
            else if (selectedVariable.type == EDataType._num)
              ret.Add(new Command(ECommand.ASSIGN_VARBIT, addr, DecodeNumValue(items[pos + 1].data, items[pos + 1].line)));
            else
              Program.Error($"Type '{selectedVariable.type}' is not supported to be assigned here.", items[pos + 1].line);
          }
          else
          {
            var x = (from v in currentContext.declaredVars where v.name == items[pos + 1].data select v);

            if (!x.Any())
              Program.Error($"Expected variable name or value but got '{items[pos + 1].data}'.", items[pos + 1].line);

            if (x.First().type != selectedVariable.type)
              Program.Error($"Variable '{items[pos + 1].data}' is of type '{x.First().type}' but should be '{selectedVariable.type}'.", items[pos + 1].line);

            addr = x.First().addr;
          }

          switch (items[pos].data)
          {
            case "+":
              ret.Add(new Command(ECommand.ADD, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "-":
              ret.Add(new Command(ECommand.SUBTRACT, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "*":
              ret.Add(new Command(ECommand.MULTIPLY, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "/":
              ret.Add(new Command(ECommand.DIVIDE, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "%":
              ret.Add(new Command(ECommand.MODULO, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "&":
              ret.Add(new Command(ECommand.AND, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "|":
              ret.Add(new Command(ECommand.OR, addr, selectedVariable.addr, selectedVariable.type));
              break;

            case "^":
              ret.Add(new Command(ECommand.XOR, addr, selectedVariable.addr, selectedVariable.type));
              break;

            default:
              Program.Error($"Operator '{items[pos].data}' is not supported.", items[pos].line);
              break;
          }

          pos += 2;
        }
        else if (selectedVariable == null && items[pos].itemType == ESyntaxItemType.NAME)
        {
          if (items.Count > pos + 1 && items[pos + 1].itemType == ESyntaxItemType.LINE_END)
          {
            var labels = (from l in currentContext.declaredLabels where l.name == items[pos].data select l);

            if (labels.Count() > 1)
              Program.What($"The label '{items[pos].data}' has been defined multiple times. You seem to be on the right track.", items[pos].line);

            foreach (Label l in labels)
              Program.Warning($"The label '{items[pos].data}' was already predefined in line {l.predefinedLine}.", items[pos].line);

            if (!labels.Any())
            {
              Label label = new Label() { name = items[pos].data, position = null, predefinedLine = items[pos].line, index = (uint)currentContext.declaredLabels.Count };

              currentContext.declaredLabels.Add(label);

              ret.Add(new Command(ECommand.__PREDEFINE_LABEL, label.index));
            }
            pos += 2;
          }
          else
          {
            var x = (from v in currentContext.declaredVars where v.name == items[pos].data select v);

            if (x.Any())
            {
              if (x.Count() > 1)
                Program.What($"Somehow you were able to define multiple variables with the name '{items[pos].data}'. That doesn't sound very healthy but you surely know what you're doing, right? We're gonna grab the last one for you.", items[pos].line);

              if (items.Count > pos + 1 && items[pos + 1].itemType == ESyntaxItemType.ASSIGN)
              {
                selectedVariable = x.Last();
                pos += 2;
              }
              else
              {
                Program.Error($"Unexpected token '{items[pos].data}'.", items[pos].line);
              }
            }
            else
            {
              Program.Error($"Unexpected token '{items[pos].data}'.", items[pos].line);
            }
          }
        }
        else if(selectedVariable == null && items.Count > pos + 2 && items[pos].itemType == ESyntaxItemType.HERE && items[pos + 1].itemType == ESyntaxItemType.NAME && items[pos + 2].itemType == ESyntaxItemType.LINE_END)
        {
          var x = (from v in currentContext.declaredLabels where v.name == items[pos + 1].data select v);

          if(x.Any())
          {
            if(x.Count() > 1)
              Program.What($"Somehow you were able to define multiple labels with the name '{items[pos + 1].data}'. That's weird, but we're just gonna grab the last one for you.", items[pos + 1].line);

            Label label = x.Last();

            if (label.position.HasValue)
              Program.Error($"The label '{label.name}' has already been defined in line {label.definedLine}.", items[pos + 1].line);

            label.definedLine = items[pos + 1].line;
            label.position = ret.Count;

            ret.Add(new Command(ECommand.__SET_LABEL, label.index, (uint)label.position));
          }
          else
          {
            Label label = new Label() { name = items[pos + 1].data, position = ret.Count, predefinedLine = items[pos + 1].line, index = (uint)currentContext.declaredLabels.Count, definedLine = items[pos + 1].line };
            currentContext.declaredLabels.Add(label);
            ret.Add(new Command(ECommand.__PREDEFINE_LABEL, label.index));
            ret.Add(new Command(ECommand.__SET_LABEL, label.index, (uint)label.position));
          }

          pos += 3;
        }
        else
        {
          Program.Error($"Unexpected token '{items[pos].data}'.", items[pos].line);
        }
      }

      foreach (Label l in currentContext.declaredLabels)
        if (!l.position.HasValue)
          Program.Error($"The label '{l.name}' was predefined but has never been set.", l.predefinedLine);

      return ret;
    }

    private byte Decode3BitValue(string data, int line)
    {
      if (data.Length != 3)
        Program.Error($"Invalid length for a 3bit value. Only 3 characters that are '0' or '1' supported.", line);

      if ((from c in data where c != '0' && c != '1' select c).Any())
        Program.Error($"Unsupported Character in 3bit Value '{data}'. Supported characters just include '0' and '1'.", line);

      byte ret = 0;

      for (int i = 0; i < data.Length; i++)
      {
        ret |= (byte)(data[i] == '1' ? (1 << (data.Length - i - 1)) : 0);
      }

      return ret;
    }

    private long DecodeNumValue(string data, int line)
    {
      if((from c in data where !char.IsDigit(c) select c).Any())
      {
        if (data.Length > 2 && data[1] == 'x')
        {
          long ret = 0;
          byte b = 1;
          string lower = data.ToLower();

          for (int i = data.Length - 1; i > 1; i--)
          {
            int num = 0;

            if (char.IsDigit(data[i]))
              num = data[i] - '0';
            else if (lower[i] >= 'a' && lower[i] <= 'f')
              num = data[i] - 'a' + 10;
            else
              Program.Error($"Unsupported Character '{data[i]}' in Hexadecimal 3bit Value '{data}'.", line);

            ret += (((num & 1) == 1) ? b : 0);
            b <<= 1;

            ret += (((num & 2) == 2) ? b : 0);
            b <<= 1;

            ret += (((num & 4) == 4) ? b : 0);
            b <<= 1;

            ret += (((num & 8) == 8) ? b : 0);
            b <<= 1;
          }

          return ret;
        }
        else if (data.Length > 2 && data[1] == 'b')
        {
          long ret = 0;
          byte b = 1;

          for (int i = data.Length - 1; i > 1; i--)
          {
            if (data[i] == '1')
              ret += b;

            b <<= 1;
          }

          return ret;
        }
        else
        {
          Program.Error($"Unsupported varbit Value format '{data}'.", line);
          return 0;
        }
      }
      else
      {
        long ret = 0;

        if(!long.TryParse(data, out ret))
        {
          Program.Error($"Unsupported varbit Value format '{data}'.", line);
          return 0;
        }

        return ret;
      }
    }

    private Varbit DecodeVarbitValue(string data, int line)
    {
      if((from c in data where !char.IsDigit(c) select c).Any())
      {
        if (data.Length > 2 && data[1] == 'p')
        {
          return DecodeVarbitValue(data.Replace('p', 'b').Replace('l', '0').Replace('r', '1'), line);
        }
        else if (data.Length > 1 && data[1] == 'b')
        {
          string s = data.Substring(2);

          if ((from m in s where !(m == '0' || m == '1') select m).Any())
            Program.Error("0b or 0l based varbit values can only contain 0 or 1 for 0b and 0, 1, l or r for 0l.", line);

          List<byte> ret = new List<byte>();

          var k = (from x in s select x == '1').Reverse().ToArray();
          byte b = 0;
          byte c = 0;

          for (int i = 0; i < k.Length; i++)
          {
            c |= (byte)((byte)(k[i] ? (1 << b) : 0));
            b++;

            if (b == 8 || i + 1 == k.Length)
            {
              ret.Add(c);
              c = 0;
              b = 0;
            }
          }

          return new Varbit(ret, k.Length);
        }
        else if (data.Length > 2 && data[1] == 'x')
        {
          string s = "";
          string lower = data.ToLower();

          for (int i = data.Length - 1; i > 1; i--)
          {
            int num = 0;

            if (char.IsDigit(data[i]))
              num = data[i] - '0';
            else if (lower[i] >= 'a' && lower[i] <= 'f')
              num = data[i] - 'a' + 10;
            else
              Program.Error($"Unsupported Character '{data[i]}' in Hexadecimal varbit Value '{data}'.", line);

            s = (((num & 1) == 1) ? "1" : "0") + s;
            s = (((num & 2) == 2) ? "1" : "0") + s;
            s = (((num & 4) == 4) ? "1" : "0") + s;
            s = (((num & 8) == 8) ? "1" : "0") + s;
          }

          return DecodeVarbitValue("0b" + s, line);
        }
        else
        {
          Program.Error($"Unsupported varbit Value format '{data}'.", line);
          return null;
        }
      }
      else
      {
        string s = "";

        BigInteger x = BigInteger.Parse(data);
        BigInteger k = BigInteger.One;

        while (k <= x)
        {
          if ((x & k) != 0)
          {
            x -= k;
            s = "1" + s;
          }
          else
          {
            s = "0" + s;
          }

          k = k << 1;
        }

        return DecodeVarbitValue("0b" + s, line);
      }
    }
  }

  public class Varbit
  {
    public List<byte> bytes;
    public int length;

    public Varbit(List<byte> bytes, int length)
    {
      if (length < 0)
        Program.Error("Varbit of negative length. Error.", -1);

      this.length = length;
      this.bytes = bytes;
    }
  }

  static class ExtentionMethods
  {
    public static IEnumerable<string> SplitKeep(this string s, string [] delim)
    {
      var ret = new List<string>();

      int length = s.Length;

      for (int i = 0; i < length; i++)
      {
        for (int j = 0; j < delim.Length; j++)
        {
          if(s.Length >= delim[j].Length + i)
          {
            if(s.Substring(i, delim[j].Length) == delim[j])
            {
              if (i > 0)
                ret.Add(s.Substring(0, i));

              ret.Add(s.Substring(i, delim[j].Length));

              s = s.Substring(i + delim[j].Length);
              length = s.Length;
              i = -1;
              break;
            }
          }
        }
      }

      ret.Add(s);

      for (int i = ret.Count - 1; i >= 0; i--)
        if (string.IsNullOrWhiteSpace(ret[i]))
          ret.RemoveAt(i);

      return ret;
    }

    public static string ToVarbitString(this List<byte> s)
    {
      if (s == null)
        return "<NULL>";

      string ret = "";

      for (int i = 0; i < s.Count; i++)
      {
        byte c = s[i];

        for (int j = 0; j < 8; j++)
        {
          ret = (((c & (1 << j)) != 0) ? "1" : "0") + ret;
        }
      }

      return ret;
    }

    static char[] hexlut = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

    public static string ToVarbitNum(this List<byte> s)
    {
      if (s == null)
        return "<NULL>";

      string ret = "";

      for (int i = s.Count - 1; i >= 0; i--)
      {
        byte c = s[i];
        
        ret += hexlut[((c >> 4) & 0xF)];
        ret += hexlut[((c >> 0) & 0xF)];
      }

      return "0x" + ret;
    }
  }
}
