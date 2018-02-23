using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blart
{
  static class Memory
  {
    static Node topNode;

    public class Node
    {
      bool isLeaf = true;
      Node[] children;
      object data = null;
    }
  }

  public struct Num
  {
    public bool isNaN;
    public long Number;

    public Num(long num)
    {
      isNaN = false;
      Number = num;
    }

    public Num(bool isNaN)
    {
      this.isNaN = isNaN;
      Number = isNaN ? 0 : 1;
    }
  }

  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("== Blah Execution Host ==\n");
    }
  }
}
