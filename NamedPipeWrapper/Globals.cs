using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NamedPipeWrapper
{
    public static class Globals
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            //PropertyNamingPolicy = null,
            //WriteIndented = true,
            //AllowTrailingCommas = true,
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.Preserve,  //cyclical references
        };
    }
}
