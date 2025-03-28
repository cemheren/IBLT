using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBLT.Sim
{
    public class LinkProcessor
    {
        public Dictionary<string, FaultTolerantIBLT> checksums = new(StringComparer.OrdinalIgnoreCase);

        public void AddLinkWithErrorDetection(string source, string scope, string target, string name)
        {
            if (this.checksums.TryGetValue(this.EncodeKey(scope, target), out var iblt))
            {
                var previous = iblt.Clone();
                
                iblt.InsertString(this.EncodeKey(scope, target), this.EncodeValue(scope, target, name));
                
                var next = iblt.Clone();

                next.Substract(previous);

                var missedList = next.ListStrings()?.Where(kvp => kvp.Item1 != this.EncodeKey(scope, target));

                if (missedList?.Any() == true)
                {
                    throw new Exception($"Detected missed links \n {string.Join('\n', missedList)}");
                }
            }
            else
            {
                iblt = new FaultTolerantIBLT(4);
                iblt.InsertString(this.EncodeKey(scope, target), this.EncodeValue(scope, target, name));
                this.checksums.Add(this.EncodeKey(scope, target), iblt);
            }
        }

        public void AddLink(string source, string scope, string target, string name)
        {
            if (this.checksums.TryGetValue(this.EncodeKey(scope, target), out var iblt))
            {
                iblt.InsertString(this.EncodeKey(scope, target), this.EncodeValue(scope, target, name));
            }
            else
            {
                iblt = new FaultTolerantIBLT(4);
                iblt.InsertString(this.EncodeKey(scope, target), this.EncodeValue(scope, target, name));
                this.checksums.Add(this.EncodeKey(scope, target), iblt);
            }
        }

        public void RemoveLink(string source, string scope, string target, string name)
        {
            if (this.checksums.TryGetValue(this.EncodeKey(scope, target), out var iblt))
            {
                iblt.DeleteString(this.EncodeKey(scope, target), this.EncodeValue(scope, target, name));
            }
            else
            {
                iblt = new FaultTolerantIBLT(4);
                iblt.DeleteString(this.EncodeKey(scope, target), this.EncodeValue(scope, target, name));
                this.checksums.Add(this.EncodeKey(scope, target), iblt);
            }
        }

        private string EncodeKey(string scope, string targetname)
        {
            return $"{scope} : {targetname}";
        }


        private string EncodeValue(string scope, string targetname, string linkName)
        {
            return $"{scope} : {targetname} : {linkName}";
        }
    }
}
