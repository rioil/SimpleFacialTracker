using BuildSoft.VRChat.Osc.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFacialTracker
{
    internal class VRCOscClient
    {
        public VRCOscClient()
        {
            // TODO https://github.com/ChanyaVRC/VRCOscLib
            var config = OscAvatarConfig.CreateAtCurrent();
        }
    }
}
