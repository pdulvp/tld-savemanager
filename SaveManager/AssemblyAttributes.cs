using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveManager
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class GitRepositoryAttribute : Attribute
    {
        public string Repository { get; }
        public GitRepositoryAttribute() : this(string.Empty) { }
        public GitRepositoryAttribute(string txt) { Repository = txt; }
    
    }
}
