namespace ViscaCamLink.Configuration
{
    using System;

    public interface ILayoutConfiguration
    {
        public Boolean MemoryContainerVisible { get; set; }

        public Boolean MoveContainerVisible { get; set; }

        public Boolean ZoomContainerVisible { get; set; }
    }

    public interface IConnectionConfiguration
    {
        public String Ip { get; set; }

        public Int32 Port { get; set; }
    }

    public interface IControlConfiguration
    {
        public Int32 PanTiltSpeed { get; set; }

        public Int32 ZoomSpeed { get; set; }
    }

    public interface IMemoryConfiguration
    {
        public String Slot0Name { get; set; }

        public String Slot1Name { get; set; }

        public String Slot2Name { get; set; }

        public String Slot3Name { get; set; }

        public String Slot4Name { get; set; }
                        
        public String Slot5Name { get; set; }

        public String Slot6Name { get; set; }

        public String Slot7Name { get; set; }

        public String Slot8Name { get; set; }

        public String Slot9Name { get; set; }
    }

    public interface IHotKeyConfiguration
    {
    }
}
