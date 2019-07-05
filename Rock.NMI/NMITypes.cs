using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Rock.NMI
{
    public class NMITypes
    {
        [Serializable()]
        [XmlType( AnonymousType = true )]
        [XmlRoot( Namespace = "", IsNullable = false, ElementName = "sale" )]
        public class sale
        {
            /// <remarks/>
            [XmlElement( "api-key" )]
            public string ApiKey { get; set; }

            /// <remarks/>
            [XmlElement( "ip-address" )]
            public string IPAddress { get; set; }

            /// <remarks/>
            [XmlElement( "currency" )]
            public string Currency { get; set; } = "USD";

            /// <remarks/>
            [XmlElement( "amount" )]
            public decimal amount { get; set; }

            /// <remarks/>
            [XmlElement( "order-description" )]
            public object orderdescription { get; set; }

            /// <remarks/>
            [XmlElement( "tax-amount" )]
            public decimal taxamount { get; set; }

            /// <remarks/>
            [XmlElement( "shipping-amount" )]
            public decimal shippingamount { get; set; }

            /// <remarks/>
            [XmlElement( "add-customer" )]
            public object addcustomer { get; set; }

            /// <remarks/>
            [XmlElement( "redirect-url" )]
            public string redirecturl { get; set; }

            /// <remarks/>
            [XmlElement( "billing" )]
            public saleBilling billing { get; set; }
        }

        /// <remarks/>
        [Serializable()]
        [XmlType( AnonymousType = true )]
        public class saleBilling
        {

            /// <remarks/>
            [XmlElement( "first-name" )]
            public string firstname { get; set; }

            /// <remarks/>
            [XmlElement( "last-name" )]
            public string lastname { get; set; }

            /// <remarks/>
            public string address1 { get; set; }

            /// <remarks/>
            public string address2 { get; set; }

            /// <remarks/>
            public string city { get; set; }

            /// <remarks/>
            public string state { get; set; }

            /// <remarks/>
            public string postal { get; set; }

            /// <remarks/>
            public string country { get; set; }

            /// <remarks/>
            public string phone { get; set; }

            /// <remarks/>
            public string email { get; set; }
        }



        // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
        /// <remarks/>
        [Serializable()]
        [XmlType( AnonymousType = true )]
        [XmlRoot( "complete-action", Namespace = "", IsNullable = false )]
        public partial class completeaction
        {
            private string tokenidField;

            /// <remarks/>
            [XmlElement( "api-key" )]
            public string apikey { get; set; }

            /// <remarks/>
            [XmlElement( "token-id" )]
            public string tokenid
            {
                get
                {
                    return this.tokenidField;
                }
                set
                {
                    this.tokenidField = value;
                }
            }
        }




    }
}
