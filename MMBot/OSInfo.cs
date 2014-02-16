
using System.Runtime.InteropServices;
using System;

namespace CSharp411
{
    /// <summary>
    /// Provides detailed information about the host operating system.
    /// </summary>
    public sealed class OSInfo
    {
        private OSInfo()
        {
        }
        #region "BITS"
        /// <summary>
        /// Determines if the current application is 32 or 64-bit.
        /// </summary>
        public static int Bits
        {
            get { return IntPtr.Size * 8; }
        }
        #endregion

        #region "EDITION"
        private static string s_Edition;
        /// <summary>
        /// Gets the edition of the operating system running on this computer.
        /// </summary>
        public static string Edition
        {
            get
            {
                if (s_Edition != null)
                {
                    return s_Edition;
                }
                //***** RETURN *****//
                string edition__1 = String.Empty;

                OperatingSystem osVersion = Environment.OSVersion;
                OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
                osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

                if (GetVersionEx(ref osVersionInfo))
                {
                    int majorVersion = osVersion.Version.Major;
                    int minorVersion = osVersion.Version.Minor;
                    byte productType = osVersionInfo.wProductType;
                    short suiteMask = osVersionInfo.wSuiteMask;

                    //#Region "VERSION 4"
                    if (majorVersion == 4)
                    {
                        if (productType == VER_NT_WORKSTATION)
                        {
                            // Windows NT 4.0 Workstation
                            edition__1 = "Workstation";
                        }
                        else if (productType == VER_NT_SERVER)
                        {
                            if ((suiteMask & VER_SUITE_ENTERPRISE) != 0)
                            {
                                // Windows NT 4.0 Server Enterprise
                                edition__1 = "Enterprise Server";
                            }
                            else
                            {
                                // Windows NT 4.0 Server
                                edition__1 = "Standard Server";
                            }
                        }
                        //#End Region

                        //#Region "VERSION 5"
                    }
                    else if (majorVersion == 5)
                    {
                        if (productType == VER_NT_WORKSTATION)
                        {
                            if ((suiteMask & VER_SUITE_PERSONAL) != 0)
                            {
                                // Windows XP Home Edition
                                edition__1 = "Home";
                            }
                            else
                            {
                                // Windows XP / Windows 2000 Professional
                                edition__1 = "Professional";
                            }
                        }
                        else if (productType == VER_NT_SERVER)
                        {
                            if (minorVersion == 0)
                            {
                                if ((suiteMask & VER_SUITE_DATACENTER) != 0)
                                {
                                    // Windows 2000 Datacenter Server
                                    edition__1 = "Datacenter Server";
                                }
                                else if ((suiteMask & VER_SUITE_ENTERPRISE) != 0)
                                {
                                    // Windows 2000 Advanced Server
                                    edition__1 = "Advanced Server";
                                }
                                else
                                {
                                    // Windows 2000 Server
                                    edition__1 = "Server";
                                }
                            }
                            else
                            {
                                if ((suiteMask & VER_SUITE_DATACENTER) != 0)
                                {
                                    // Windows Server 2003 Datacenter Edition
                                    edition__1 = "Datacenter";
                                }
                                else if ((suiteMask & VER_SUITE_ENTERPRISE) != 0)
                                {
                                    // Windows Server 2003 Enterprise Edition
                                    edition__1 = "Enterprise";
                                }
                                else if ((suiteMask & VER_SUITE_BLADE) != 0)
                                {
                                    // Windows Server 2003 Web Edition
                                    edition__1 = "Web Edition";
                                }
                                else
                                {
                                    // Windows Server 2003 Standard Edition
                                    edition__1 = "Standard";
                                }
                            }
                        }
                        //#End Region

                        //#Region "VERSION 6"
                    }
                    else if (majorVersion == 6)
                    {
                        int ed = 0;
                        if (GetProductInfo(majorVersion, minorVersion, osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor, ref ed))
                        {
                            switch (ed)
                            {
                                case PRODUCT_BUSINESS:
                                    edition__1 = "Business";
                                    break;

                                case PRODUCT_BUSINESS_N:
                                    edition__1 = "Business N";
                                    break;

                                case PRODUCT_CLUSTER_SERVER:
                                    edition__1 = "HPC Edition";
                                    break;

                                case PRODUCT_DATACENTER_SERVER:
                                    edition__1 = "Datacenter Server";
                                    break;

                                case PRODUCT_DATACENTER_SERVER_CORE:
                                    edition__1 = "Datacenter Server (core installation)";
                                    break;

                                case PRODUCT_ENTERPRISE:
                                    edition__1 = "Enterprise";
                                    break;

                                case PRODUCT_ENTERPRISE_N:
                                    edition__1 = "Enterprise N";
                                    break;

                                case PRODUCT_ENTERPRISE_SERVER:
                                    edition__1 = "Enterprise Server";
                                    break;

                                case PRODUCT_ENTERPRISE_SERVER_CORE:
                                    edition__1 = "Enterprise Server (core installation)";
                                    break;

                                case PRODUCT_ENTERPRISE_SERVER_CORE_V:
                                    edition__1 = "Enterprise Server without Hyper-V (core installation)";
                                    break;

                                case PRODUCT_ENTERPRISE_SERVER_IA64:
                                    edition__1 = "Enterprise Server for Itanium-based Systems";
                                    break;

                                case PRODUCT_ENTERPRISE_SERVER_V:
                                    edition__1 = "Enterprise Server without Hyper-V";
                                    break;

                                case PRODUCT_HOME_BASIC:
                                    edition__1 = "Home Basic";
                                    break;

                                case PRODUCT_HOME_BASIC_N:
                                    edition__1 = "Home Basic N";
                                    break;

                                case PRODUCT_HOME_PREMIUM:
                                    edition__1 = "Home Premium";
                                    break;

                                case PRODUCT_HOME_PREMIUM_N:
                                    edition__1 = "Home Premium N";
                                    break;

                                case PRODUCT_HYPERV:
                                    edition__1 = "Microsoft Hyper-V Server";
                                    break;

                                case PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT:
                                    edition__1 = "Windows Essential Business Management Server";
                                    break;

                                case PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING:
                                    edition__1 = "Windows Essential Business Messaging Server";
                                    break;

                                case PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY:
                                    edition__1 = "Windows Essential Business Security Server";
                                    break;

                                case PRODUCT_SERVER_FOR_SMALLBUSINESS:
                                    edition__1 = "Windows Essential Server Solutions";
                                    break;

                                case PRODUCT_SERVER_FOR_SMALLBUSINESS_V:
                                    edition__1 = "Windows Essential Server Solutions without Hyper-V";
                                    break;

                                case PRODUCT_SMALLBUSINESS_SERVER:
                                    edition__1 = "Windows Small Business Server";
                                    break;

                                case PRODUCT_STANDARD_SERVER:
                                    edition__1 = "Standard Server";
                                    break;

                                case PRODUCT_STANDARD_SERVER_CORE:
                                    edition__1 = "Standard Server (core installation)";
                                    break;

                                case PRODUCT_STANDARD_SERVER_CORE_V:
                                    edition__1 = "Standard Server without Hyper-V (core installation)";
                                    break;

                                case PRODUCT_STANDARD_SERVER_V:
                                    edition__1 = "Standard Server without Hyper-V";
                                    break;

                                case PRODUCT_STARTER:
                                    edition__1 = "Starter";
                                    break;

                                case PRODUCT_STORAGE_ENTERPRISE_SERVER:
                                    edition__1 = "Enterprise Storage Server";
                                    break;

                                case PRODUCT_STORAGE_EXPRESS_SERVER:
                                    edition__1 = "Express Storage Server";
                                    break;

                                case PRODUCT_STORAGE_STANDARD_SERVER:
                                    edition__1 = "Standard Storage Server";
                                    break;

                                case PRODUCT_STORAGE_WORKGROUP_SERVER:
                                    edition__1 = "Workgroup Storage Server";
                                    break;

                                case PRODUCT_UNDEFINED:
                                    edition__1 = "Unknown product";
                                    break;

                                case PRODUCT_ULTIMATE:
                                    edition__1 = "Ultimate";
                                    break;

                                case PRODUCT_ULTIMATE_N:
                                    edition__1 = "Ultimate N";
                                    break;

                                case PRODUCT_WEB_SERVER:
                                    edition__1 = "Web Server";
                                    break;

                                case PRODUCT_WEB_SERVER_CORE:
                                    edition__1 = "Web Server (core installation)";
                                    break;

                            }
                        }
                        //#End Region
                    }
                }

                s_Edition = edition__1;
                return edition__1;
            }
        }
        #endregion

        #region "NAME"
        private static string s_Name;
        /// <summary>
        /// Gets the name of the operating system running on this computer.
        /// </summary>
        public static string Name
        {
            get
            {
                if (s_Name != null)
                {
                    return s_Name;
                }
                //***** RETURN *****//
                string name__1 = "unknown";

                OperatingSystem osVersion = Environment.OSVersion;
                OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
                osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

                if (GetVersionEx(ref osVersionInfo))
                {
                    int majorVersion = osVersion.Version.Major;
                    int minorVersion = osVersion.Version.Minor;

                    switch (osVersion.Platform)
                    {
                        case PlatformID.Win32Windows:
                            if (true)
                            {
                                if (majorVersion == 4)
                                {
                                    string csdVersion = osVersionInfo.szCSDVersion;
                                    switch (minorVersion)
                                    {
                                        case 0:
                                            if (csdVersion == "B" || csdVersion == "C")
                                            {
                                                name__1 = "Windows 95 OSR2";
                                            }
                                            else
                                            {
                                                name__1 = "Windows 95";
                                            }
                                            break;
                                        case 10:
                                            if (csdVersion == "A")
                                            {
                                                name__1 = "Windows 98 Second Edition";
                                            }
                                            else
                                            {
                                                name__1 = "Windows 98";
                                            }
                                            break;
                                        case 90:
                                            name__1 = "Windows Me";
                                            break;
                                    }
                                }
                            }

                            break;
                        case PlatformID.Win32NT:
                            if (true)
                            {
                                byte productType = osVersionInfo.wProductType;

                                switch (majorVersion)
                                {
                                    case 3:
                                        name__1 = "Windows NT 3.51";
                                        break;
                                    case 4:
                                        switch (productType)
                                        {
                                            case 1:
                                                name__1 = "Windows NT 4.0";
                                                break;
                                            case 3:
                                                name__1 = "Windows NT 4.0 Server";
                                                break;
                                        }
                                        break;
                                    case 5:
                                        switch (minorVersion)
                                        {
                                            case 0:
                                                name__1 = "Windows 2000";
                                                break;
                                            case 1:
                                                name__1 = "Windows XP";
                                                break;
                                            case 2:
                                                name__1 = "Windows Server 2003";
                                                break;
                                        }
                                        break;
                                    case 6:
                                        switch (productType)
                                        {
                                            case 1:
                                                switch (minorVersion)
                                                {
                                                    case 0:
                                                        name__1 = "Windows Vista";
                                                        break;
                                                    case 1:
                                                        name__1 = "Windows 7";
                                                        break;
                                                    case 2:
                                                        name__1 = "Windows 8";
                                                        break;
                                                    case 3:
                                                        name__1 = "Windows 8.1";
                                                        break;
                                                }
                                                break;
                                            case 3:
                                                name__1 = "Windows Server 2008";
                                                break;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                s_Name = name__1;
                return name__1;
            }
        }
        #endregion

        #region "PINVOKE"
        #region "GET"
        #region "PRODUCT INFO"
        [DllImport("Kernel32.dll")]
        static internal extern bool GetProductInfo(int osMajorVersion, int osMinorVersion, int spMajorVersion, int spMinorVersion, ref int edition);
        #endregion

        #region "VERSION"
        [DllImport("kernel32.dll")]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);
        #endregion
        #endregion

        #region "OSVERSIONINFOEX"
        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
        #endregion

        #region "PRODUCT"
        private const int PRODUCT_UNDEFINED = 0x0;
        private const int PRODUCT_ULTIMATE = 0x1;
        private const int PRODUCT_HOME_BASIC = 0x2;
        private const int PRODUCT_HOME_PREMIUM = 0x3;
        private const int PRODUCT_ENTERPRISE = 0x4;
        private const int PRODUCT_HOME_BASIC_N = 0x5;
        private const int PRODUCT_BUSINESS = 0x6;
        private const int PRODUCT_STANDARD_SERVER = 0x7;
        private const int PRODUCT_DATACENTER_SERVER = 0x8;
        private const int PRODUCT_SMALLBUSINESS_SERVER = 0x9;
        private const int PRODUCT_ENTERPRISE_SERVER = 0xa;
        private const int PRODUCT_STARTER = 0xb;
        private const int PRODUCT_DATACENTER_SERVER_CORE = 0xc;
        private const int PRODUCT_STANDARD_SERVER_CORE = 0xd;
        private const int PRODUCT_ENTERPRISE_SERVER_CORE = 0xe;
        private const int PRODUCT_ENTERPRISE_SERVER_IA64 = 0xf;
        private const int PRODUCT_BUSINESS_N = 0x10;
        private const int PRODUCT_WEB_SERVER = 0x11;
        private const int PRODUCT_CLUSTER_SERVER = 0x12;
        private const int PRODUCT_HOME_SERVER = 0x13;
        private const int PRODUCT_STORAGE_EXPRESS_SERVER = 0x14;
        private const int PRODUCT_STORAGE_STANDARD_SERVER = 0x15;
        private const int PRODUCT_STORAGE_WORKGROUP_SERVER = 0x16;
        private const int PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x17;
        private const int PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x18;
        private const int PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x19;
        private const int PRODUCT_HOME_PREMIUM_N = 0x1a;
        private const int PRODUCT_ENTERPRISE_N = 0x1b;
        private const int PRODUCT_ULTIMATE_N = 0x1c;
        private const int PRODUCT_WEB_SERVER_CORE = 0x1d;
        private const int PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x1e;
        private const int PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x1f;
        private const int PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x20;
        private const int PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x23;
        private const int PRODUCT_STANDARD_SERVER_V = 0x24;
        private const int PRODUCT_ENTERPRISE_SERVER_V = 0x26;
        private const int PRODUCT_STANDARD_SERVER_CORE_V = 0x28;
        private const int PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x29;
        #endregion
        private const int PRODUCT_HYPERV = 0x2a;

        #region "VERSIONS"
        private const int VER_NT_WORKSTATION = 1;
        private const int VER_NT_DOMAIN_CONTROLLER = 2;
        private const int VER_NT_SERVER = 3;
        private const int VER_SUITE_SMALLBUSINESS = 1;
        private const int VER_SUITE_ENTERPRISE = 2;
        private const int VER_SUITE_TERMINAL = 16;
        private const int VER_SUITE_DATACENTER = 128;
        private const int VER_SUITE_SINGLEUSERTS = 256;
        private const int VER_SUITE_PERSONAL = 512;
        #endregion
        private const int VER_SUITE_BLADE = 1024;
        #endregion

        #region "SERVICE PACK"
        /// <summary>
        /// Gets the service pack information of the operating system running on this computer.
        /// </summary>
        public static string ServicePack
        {
            get
            {
                string servicePack__1 = String.Empty;
                OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();

                osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

                if (GetVersionEx(ref osVersionInfo))
                {
                    servicePack__1 = osVersionInfo.szCSDVersion;
                }

                return servicePack__1;
            }
        }
        #endregion

        #region "VERSION"
        #region "BUILD"
        /// <summary>
        /// Gets the build version number of the operating system running on this computer.
        /// </summary>
        public static int BuildVersion
        {
            get { return Environment.OSVersion.Version.Build; }
        }
        #endregion

        #region "FULL"
        #region "STRING"
        /// <summary>
        /// Gets the full version string of the operating system running on this computer.
        /// </summary>
        public static string VersionString
        {
            get { return Environment.OSVersion.Version.ToString(); }
        }
        #endregion

        #region "VERSION"
        /// <summary>
        /// Gets the full version of the operating system running on this computer.
        /// </summary>
        public static Version Version
        {
            get { return Environment.OSVersion.Version; }
        }
        #endregion
        #endregion

        #region "MAJOR"
        /// <summary>
        /// Gets the major version number of the operating system running on this computer.
        /// </summary>
        public static int MajorVersion
        {
            get { return Environment.OSVersion.Version.Major; }
        }
        #endregion

        #region "MINOR"
        /// <summary>
        /// Gets the minor version number of the operating system running on this computer.
        /// </summary>
        public static int MinorVersion
        {
            get { return Environment.OSVersion.Version.Minor; }
        }
        #endregion

        #region "REVISION"
        /// <summary>
        /// Gets the revision version number of the operating system running on this computer.
        /// </summary>
        public static int RevisionVersion
        {
            get { return Environment.OSVersion.Version.Revision; }
        }
        #endregion
        #endregion
    }
}