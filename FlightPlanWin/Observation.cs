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
		public string Metar { get; set; }
		public int? Visibility { get; set; }
		public int? Cloudbase { get; set; }
		public string ICAO { get; set; }
		public ColourState ColourState { get; set; }
		public List<ColourState> ColourStates { get; set; }
		public string ObservationAge { get; set; }
        public bool isInvalid { get; set; }

		public Observation(String ICAO, List<ColourState> ColourStates)
		{
			this.ICAO = ICAO;
			this.ColourStates = ColourStates;
			this.ObservationAge = "N/A";
			this.getObservation();
			this.parseObservation();
		}

        public void getObservation()
        {
            try {
                string itemContent = "";
                String URLString = "http://api.geonames.org/weatherIcao?ICAO=" + this.ICAO + "&username=skuffe";
                XmlTextReader reader = new XmlTextReader(URLString);

                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "observation") {
                        reader.ReadToFollowing("observation");
                        itemContent = reader.ReadInnerXml();
                    } else if (reader.Name == "status") {
                        itemContent = reader.GetAttribute(0);
                    }
                }
				this.Metar = itemContent;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message.ToString());
            }            
        }

		public void parseObservation()
		{
			Match match;
			match = Regex.Match(this.Metar, @"([0-9]{2})([0-9]{2})([0-9]{2})Z");
			if (match.Success) {
				string dateFormat = "yyyy-M-dd HH:mm";
			//	Console.WriteLine();
			//	Console.WriteLine(dateFormat);
				string dateStr = DateTime.UtcNow.Year + "-" + DateTime.UtcNow.Month + "-" + match.Groups[1] + " " + match.Groups[2] + ":" + match.Groups[3];
			//	Console.WriteLine(dateStr);
				DateTime convertedDate = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, dateFormat, CultureInfo.InvariantCulture), DateTimeKind.Utc);
				DateTime now = DateTime.Now;
				TimeSpan age = now - convertedDate.ToLocalTime();
				this.ObservationAge = String.Format("{0}h {1}m", (int)age.TotalHours, age.Minutes);
                if (age.TotalHours > 1)  {
                    this.isInvalid = true;
                }
			}
			/**
			 * CAVOK
			 */
			if (Regex.Match(this.Metar, @"(CAVOK)").Success) {
				this.ColourState = this.ColourStates[this.ColourStates.Count-1];
				return;
			}

			/**
			 * Visibility
			 */
			match = Regex.Match(this.Metar, @"(((?:(\s))([0-9]){4}(?:(\s)))|(([0-9]){4}(?=(NDV))))");
			if (match.Success) {
				this.Visibility = int.Parse(match.Groups[1].Value.Trim());
			} else {
				match = Regex.Match(this.Metar, @"([0-9]{1,2})(?:SM)|([0-9])/([0-9])(?=SM)");
				if (match.Success) {
					if (match.Groups[1].Value == "") {
						double first = double.Parse(match.Groups[2].Value);
						double second = double.Parse(match.Groups[3].Value);
						this.Visibility = ((int)(first/second * (double)1609.3));
					} else {
						this.Visibility = ((int)(Double.Parse(match.Groups[1].Value.Trim()) * (double)1609.3));
					}
				}
			}

			/**
			 * Cloudbase
			 */
			match = Regex.Match(this.Metar, @"(?:SCT|BKN|OVC)(([0-9]){3})");
			if (match.Success) {
				this.Cloudbase = int.Parse(match.Groups[1].Value) * 100;
			} else {
				if (Regex.Match(this.Metar, @"(?:\s)(?:SKC|CLR|NSC)(?:\s)").Success) {
					this.Cloudbase = null;
				}
			}

			/**
			 * Determine colour state
			 */
			if (this.Metar.Contains("no observation found")) {
				this.ColourState = new ColourState("N/A", 0, 0);
			} else if (!this.Metar.Contains("no observation found") && this.Visibility == null && this.Cloudbase == null) {
				this.ColourState = this.ColourStates[this.ColourStates.Count-1];
			} else {
				if (this.Visibility == null && this.Cloudbase != null) {
					foreach (ColourState state in this.ColourStates) {
						if (this.Cloudbase >= state.Cloudbase) {
							this.ColourState = state;
						}
					}
				} else if (this.Visibility != null && this.Cloudbase == null) {
					foreach (ColourState state in this.ColourStates) {
						if (this.Visibility >= state.Visibility) {
							this.ColourState = state;
						}
					}
				} else {
					ColourState colourVisibility = null;
					ColourState colourCloudbase = null;
					foreach (ColourState state in this.ColourStates) {
						if (this.Visibility >= state.Visibility) {
							colourVisibility = state;
						}

						if (this.Cloudbase >= state.Cloudbase) {
							colourCloudbase = state;
						}
					}

					int indexVisibility = this.ColourStates.IndexOf(colourVisibility);
					int indexCloudbase = this.ColourStates.IndexOf(colourCloudbase);

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
