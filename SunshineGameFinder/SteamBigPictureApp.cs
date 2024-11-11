using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunshineGameFinder
{
    public class SteamBigPictureApp : SunshineApp
    {
        public SteamBigPictureApp() {
            this.Name = "Steam Big Picture";
            this.Cmd = "steam://open/bigpicture";
            this.AutoDetach = "true";
            this.WaitAll = "true";
            this.ImagePath = "steam.png";
        }
    }
}
