using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Globalization;

namespace FlightPlanWin
{
	class Observation
	{
        /**
         * Properties declaration
         */
		public string Metar { get; set; }
		public int? Visibility { get; set; }
		public int? Cloudbase { get; set; }
		public string ICAO { get; set; }
		public ColourState ColourState { get; set; }
		public List<ColourState> ColourStates { get; set; }
		public string ObservationAge { get; set; }
        public bool isInvalid { get; set; }

        /**
        * Constants declaration
        */
        private const double MILE_TO_METER = 1609.3;
        private const int KILOMETER_TO_METER = 1000;
        private const int ONEHUNDREDFEET_TO_FEET = 100;

        ///<summary>
        ///Constructor
        ///</summary>
		public Observation(String ICAO, List<ColourState> ColourStates)
		{
            //Icao and ColourStates are parsed into the constructor upon creation of the object
			this.ICAO = ICAO;
			this.ColourStates = ColourStates;
            //The age is initially set to N/A and is later overwritten if applicable
			this.ObservationAge = "N/A";
            //Each observation is both fetched and parsed in the constructor
			this.getObservation();
			this.parseObservation();
		}

        ///<summary>
        ///getObservation method for retrieving a METAR observation based on parsed ICAO code. No return value, sets this.Metar directly
        ///</summary>
        public void getObservation()
        {
            try {
                string itemContent = "";
                //Create URL to fetch XML with Observation METAR from Geonames, ICAO code and username is parsed
                String URLString = "http://api.geonames.org/weatherIcao?ICAO=" + this.ICAO + "&username=mercantec";
                XmlTextReader reader = new XmlTextReader(URLString);

                //Read through the XML
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "observation") { //Found an element named observation
                        reader.ReadToFollowing("observation"); //Read until next observation child element is found
                        itemContent = reader.ReadInnerXml(); //Read the inner xml (content) of the element to the itemContent string variable
                    } else if (reader.Name == "status") { //Found an element named status
                        itemContent = reader.GetAttribute(0); //Status text is defined in xml as the first property of the status element
                    }
                }
				this.Metar = itemContent; //Set the object Metar property to the resulting itemContent from the XML parser
            }
            catch (Exception e) {
				throw new Exception(e.Message);
            }            
        }

        ///<summary>
        //parseObservation method. /We now have our observation, here we are parsing it and stripping out the bits and pieces we need for ColourState determination
        ///</summary>
		public void parseObservation()
		{
            //Create a match object to be used with REGEX
			Match match;

            /**
			 * Date and time
			 */
			match = Regex.Match(this.Metar, @"([0-9]{2})([0-9]{2})([0-9]{2})Z"); //Finds the datetime of the observation in the format DDHHMMZ
			if (match.Success) { //Found a match
				string dateFormat = "yyyy-M-dd HH:mm"; //Dateformat for the converter
				string dateStr = DateTime.UtcNow.Year + "-" + DateTime.UtcNow.Month + "-" + match.Groups[1] + " " + match.Groups[2] + ":" + match.Groups[3]; //Create datestring from observation
				DateTime convertedDate = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, dateFormat, CultureInfo.InvariantCulture), DateTimeKind.Utc);  //Create converted datetimeobject in UTC
				DateTime now = DateTime.Now; //Get current local time
				TimeSpan age = now - convertedDate.ToLocalTime(); //Calculate timespan. ToLocalTime() converts the UTC DateTime object to localtime i.e. adding 2 hours in Denmark (Summertime)
				this.ObservationAge = String.Format("{0}h {1}m", (int)age.TotalHours, age.Minutes); //Sets ObservationAge property of this object as a string
                //If total observation age exceeds 1 hour the observation is marked as invalid
                if (age.TotalHours > 1)  {
                    this.isInvalid = true;
                }
			}

			/**
			 * CAVOK
			 */
			if (Regex.Match(this.Metar, @"(CAVOK)").Success) { //If CAVOK is found in the METAR, no further parsing nessesary, we can set the ColourState to BLU
				this.ColourState = this.ColourStates[this.ColourStates.Count-1]; //Set ColourState to BLU, by referencing the last index number in the list wich holds the BLU ColourState object
				return; //Break the method
			}

			/**
			 * Visibility
			 */
			match = Regex.Match(this.Metar, @"(((?:(\s))([0-9]){4}(?:(\s)))|(([0-9]){4}(?=(NDV))))"); //Look for a four digit number XXXX with spaces on each side or with NDV
			if (match.Success) { //Found a match
				this.Visibility = int.Parse(match.Groups[1].Value.Trim()); //Set Visibility to the found value, parsed as an int and trimmed for spaces.
			} else {
				match = Regex.Match(this.Metar, @"([0-9]{1,2})(?:SM)|([0-9])/([0-9])(?=SM)"); //Look for visibility in Statute Miles
				if (match.Success) { //Found a match
					if (match.Groups[1].Value == "") { //If the first match group contains no character, the visibility is indicated in fractions
						double first = double.Parse(match.Groups[2].Value); //Declare first fraction
						double second = double.Parse(match.Groups[3].Value); //Declare second fraction
						this.Visibility = ((int)(first / second * MILE_TO_METER)); //Calculate and set visibilty of this object by dividing first and second and multiply by 1609.3
					} else { //If not indicated in fractions just multiply by 1609.3 and set visibility of this object
						this.Visibility = ((int)(Double.Parse(match.Groups[1].Value.Trim()) * MILE_TO_METER));
					}
				} else { //If no match was found in SM, look for KM
					match = Regex.Match(this.Metar, @"([0-9]+)KM");
					if (match.Success) { //Found a match
						this.Visibility = (int.Parse(match.Groups[1].Value) * KILOMETER_TO_METER); //Convert to meter and set visibility of this object
					}
				}
			}

			/**
			 * Cloudbase
			 */
			match = Regex.Match(this.Metar, @"(?:SCT|BKN|OVC)(([0-9]){3})"); //Look for a cloud indicator followed by a 3 digit number
			if (match.Success) { //Found a match
				this.Cloudbase = int.Parse(match.Groups[1].Value) * ONEHUNDREDFEET_TO_FEET; //Set Cloudbase of this object to the found value miltiplied by 100
			} else { //Else if SKC CLR or NSC is found the Cloudbase is set to null, indicating "Sky Clear"
				if (Regex.Match(this.Metar, @"(?:\s)(?:SKC|CLR|NSC)(?:\s)").Success) {
					this.Cloudbase = null;
				}
			}

			/**
			 * Determine colour state
			 */
			if (this.Metar.Contains("no observation found")) { //Check if no observation has been found, and in that case set ColourState to N/A
				this.ColourState = new ColourState("N/A", 0, 0);
            //If an observation has been found, but no visibilty or cloudbase has been reported, ColourState is set to BLU
			} else if (!this.Metar.Contains("no observation found") && this.Visibility == null && this.Cloudbase == null) {
				this.ColourState = this.ColourStates[this.ColourStates.Count-1];
            //If no visibility was found but a cloudbase was found, loop through the ColourState list, and until the cloudbase matches, then set the ColourState of this object
			} else {
				if (this.Visibility == null && this.Cloudbase != null) {
                    foreach (ColourState state in this.ColourStates) { //Loop through the list of ColourStates
						if (this.Cloudbase >= state.Cloudbase) {
							this.ColourState = state;
						}
					}
                //If visibility was found, but no cloudbase, do the same as above, but for visibilty instead
				} else if (this.Visibility != null && this.Cloudbase == null) {
                    foreach (ColourState state in this.ColourStates) { //Loop through the list of ColourStates
						if (this.Visibility >= state.Visibility) {
							this.ColourState = state;
						}
					}
                //If both visibilty and cloudbase can be determined, we must set an initial ColourState for both vis and clb, then finally select the lowest one
				} else {
					ColourState colourVisibility = null; //ColourState object to hold the found state for vis
                    ColourState colourCloudbase = null; //ColourState object to hold the found state for clb
					foreach (ColourState state in this.ColourStates) { //Loop through the list of ColourStates
						if (this.Visibility >= state.Visibility) {
							colourVisibility = state;
						}

						if (this.Cloudbase >= state.Cloudbase) {
							colourCloudbase = state;
						}
					}

                    //Create two integers to hold the ColourState index value of our above determined ColourStates for vis and clb
					int indexVisibility = this.ColourStates.IndexOf(colourVisibility);
					int indexCloudbase = this.ColourStates.IndexOf(colourCloudbase);

                    //The index values found above are compared. The lowest index found determines the final ColourState of this object
					if (indexVisibility < indexCloudbase) {
						this.ColourState = colourVisibility;
					} else {
						this.ColourState = colourCloudbase;
					}
				}
			}
		}
	}
}
