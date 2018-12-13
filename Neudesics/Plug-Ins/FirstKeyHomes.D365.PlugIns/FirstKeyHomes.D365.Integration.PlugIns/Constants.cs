using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstKeyHomes.D365.Integration.PlugIns
{
    public class Constants
    {
        public const string TARGET = "Target";
        public const string POST_IMAGE = "PostImage";

        public static class MessageNames
        {
            public const string Create = "Create";
            public const string Update = "Update";
            public const string Win = "Win";
            public const string SetState = "SetState";
            public const string SetStateDynamic = "SetStateDynamicEntity";
        }
    }
}
