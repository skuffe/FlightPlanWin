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
                string itemContent = "";
                String URLString = "http://api.geonames.org/weatherIcao?ICAO=" + icao + "&username=bigherman";
                XmlTextReader reader = new XmlTextReader(URLString);

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "observation")
                    {
                        reader.ReadToFollowing("observation");
                        itemContent = reader.ReadInnerXml();
                    }
                    else if (reader.Name == "status")
                    {
                        itemContent = reader.GetAttribute(0);
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
