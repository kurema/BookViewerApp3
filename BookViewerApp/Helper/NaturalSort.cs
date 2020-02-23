using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Helper
{
    public class NaturalSort
    {
        public class NaturalList:IComparable<NaturalList>
        {
            private List<NaturalMember> Content = new List<NaturalMember>();

            public int CompareTo(NaturalList other)
            {
                int i = 0;
                while(i<Content.Count && i < other.Content.Count)
                {
                    int temp = Content[i].CompareTo(other.Content[i]);
                    if (temp != 0)
                    {
                        return temp;
                    }
                    i++;
                }
                return Content.Count.CompareTo(other.Content.Count);
            }

            public NaturalList(string arg)
            {
                StringBuilder sb = new StringBuilder();
                bool? isnumber=null;
                for(int i = 0; i < arg.Count(); i++)
                {
                    if (Char.IsDigit(arg[i]) &&isnumber!=false)
                    {
                        isnumber = true;
                        sb.Append(arg[i]);
                    }
                    else if(Char.IsDigit(arg[i])){
                        isnumber = true;
                        Content.Add(new NaturalMember(sb.ToString()));
                        sb = new StringBuilder();
                        sb.Append(arg[i]);
                    }else if(isnumber!=true)
                    {
                        isnumber = false;
                        sb.Append(arg[i]);
                    }
                    else
                    {
                        isnumber = false;
                        Content.Add(new NaturalMember(sb.ToString()));
                        sb = new StringBuilder();
                        sb.Append(arg[i]);
                    }
                }
                Content.Add(new NaturalMember(sb.ToString()));
            }

        }

        public static int NaturalCompare(string a,string b)
        {
            return (new NaturalList(a)).CompareTo(new NaturalList(b));
        }

        public class NaturalMember:IComparable<NaturalMember>
        {
            public NaturalKind Kind { get; private set; }

            public string Content { get; private set; }

            public int? Number { get; private set; }

            public NaturalMember(string arg)
            {
                Content = arg;
                int result;
                if(Int32.TryParse(arg,out result))
                {
                    Number = result;
                    Kind = NaturalKind.Number;
                }else
                {
                    Number = null;
                    Kind = NaturalKind.String;
                }
            }

            public int CompareTo(NaturalMember other)
            {
                if (this.Kind == NaturalKind.Number && other.Kind == NaturalKind.Number)
                {
                    if (this.Number != null && other.Number != null)
                        return this.Number.Value.CompareTo(other.Number.Value);
                    else return this.Number == null && other.Number == null?0:1;
                }
                else
                {
                    return this.Content.CompareTo(other.Content);
                }
            }
        }

        public enum NaturalKind
        {
            Number,String
        }
    }
}
