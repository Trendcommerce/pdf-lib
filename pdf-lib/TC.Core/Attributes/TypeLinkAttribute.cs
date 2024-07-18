using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.Attributes
{
    // Type-Link-Attribute (04.12.2022, SME)
    public class TypeLinkAttribute: System.Attribute
    {
        public readonly Type TypeLink;
        public TypeLinkAttribute(Type typeLink)
        {
            TypeLink = typeLink;
        }
    }
}
