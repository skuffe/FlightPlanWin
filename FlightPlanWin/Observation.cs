using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Media;

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

		public Observation(String ICAO, List<ColourState> ColourStates)
		{
			this.ICAO = ICAO;
			this.ColourStates = ColourStates;
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
				match = Regex.Match(this.Metar, @"([0-9]{1,2})(?:SM)");
				if (match.Success) {
					this.Visibility = ((int)(Double.Parse(match.Groups[1].Value.Trim()) * (double)1609.3));
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
				this.ColourState = new ColourState(Colors.White, "N/A", 0, 0);
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
