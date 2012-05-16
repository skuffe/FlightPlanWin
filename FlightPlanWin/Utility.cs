using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FlightPlanWin
{
    public class Utility
    {
        public static string getObservation(string icao)
        {
            try
            {
                icao = "EBBE";
                string itemContent = "";
                String URLString = "http://api.geonames.org/weatherIcao?ICAO=" + icao + "&username=bigherman";
                XmlTextReader reader = new XmlTextReader(URLString);

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "observation")
                    {
                        XmlReader inner = reader.ReadSubtree();
                        while (inner.Read())
                        {
                            if (inner.NodeType == XmlNodeType.Element && inner.Name == "observation")
                            {
                                itemContent = inner.ReadString();
                            }
                        }
                    }
                }
                return itemContent;
            }
            catch (Exception e)
            {
                return e.Message.ToString();
            }            
        }
    }
}
