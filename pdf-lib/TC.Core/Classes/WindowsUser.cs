using TC.Functions;
using TC.Interfaces;

namespace TC.Classes
{
    public class WindowsUser: IUserInfo
    {
        // New Instance (03.02.2024, SME)
        public WindowsUser() 
        {
            Name = CoreFC.GetUserName();
            Caption = Name;
        }

        // ToString
        public override string ToString()
        {
            return Caption;
        }

        // Properties
        public string Name { get; }
        public string Caption { get; }
    }
}
