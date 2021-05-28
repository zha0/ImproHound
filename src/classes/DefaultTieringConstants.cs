﻿namespace ImproHound.classes
{
    public static class DefaultTieringConstants
    {
        public static WellKnownADObject[] WellKnownADObjects = {

            // Well-known Tier 0 SIDs
            new WellKnownADObject("S-1-5-17", "IUSR", "0"),
            new WellKnownADObject("S-1-5-18", "Local System", "0"),
            new WellKnownADObject("S-1-5-32-544", "Administrators", "0"),
            new WellKnownADObject("S-1-5-32-547", "Power Users", "0"),
            new WellKnownADObject("S-1-5-32-548", "Account Operators", "0"),
            new WellKnownADObject("S-1-5-32-549", "Server Operators", "0"),
            new WellKnownADObject("S-1-5-32-550", "Print Operators", "0"),
            new WellKnownADObject("S-1-5-32-551", "Backup Operators", "0"),
            new WellKnownADObject("S-1-5-32-552", "Replicator", "0"),
            new WellKnownADObject("S-1-5-32-555", "Remote Desktop Users", "0"),
            new WellKnownADObject("S-1-5-32-556", "Network Configuration Operators", "0"),
            new WellKnownADObject("S-1-5-32-557", "Incoming Forest Trust Builders", "0"),
            new WellKnownADObject("S-1-5-32-562", "Distributed COM Users", "0"),
            new WellKnownADObject("S-1-5-32-568", "IIS_IUSRS", "0"),
            new WellKnownADObject("S-1-5-32-569", "Cryptographic Operators", "0"),
            new WellKnownADObject("S-1-5-32-574", "Certificate Service DCOM Access", "0"),
            new WellKnownADObject("S-1-5-32-577", "RDS Management Servers", "0"),
            new WellKnownADObject("S-1-5-32-578", "Hyper-V Administrators", "0"),
            new WellKnownADObject("S-1-5-32-580", "Remote Management Users", "0"),
            new WellKnownADObject("S-1-5-32-582", "Storage Replica Administrators", "0"),
            new WellKnownADObject("S-1-5-9", "Enterprise Domain Controllers", "0"),

            // Well-known Tier 1 SIDs
            new WellKnownADObject("S-1-5-32-558", "Performance Monitor Users", "1"),
            new WellKnownADObject("S-1-5-32-559", "Performance Log Users", "1"),
            new WellKnownADObject("S-1-5-32-560", "Windows Authorization Access Group", "1"),
            new WellKnownADObject("S-1-5-32-561", "Terminal Server License Servers", "1"),
            new WellKnownADObject("S-1-5-32-573", "Event Log Readers", "1"),
            new WellKnownADObject("S-1-5-32-575", "RDS Remote Access Servers", "1"),
            new WellKnownADObject("S-1-5-32-576", "RDS Endpoint Servers", "1"),

            // Well-known Tier 2 SIDs
            new WellKnownADObject("S-1-0", "Null Authority", "2"),
            new WellKnownADObject("S-1-0-0", "Nobody", "2"),
            new WellKnownADObject("S-1-1", "World Authority", "2"),
            new WellKnownADObject("S-1-1-0", "Everyone", "2"),
            new WellKnownADObject("S-1-10", "Passport Authority", "2"),
            new WellKnownADObject("S-1-15-2-1", "All App Packages", "2"),
            new WellKnownADObject("S-1-16-0", "Untrusted Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-12288", "High Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-16384", "System Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-20480", "Protected Process Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-28672", "Secure Process Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-4096", "Low Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-8192", "Medium Mandatory Level", "2"),
            new WellKnownADObject("S-1-16-8448", "Medium Plus Mandatory Level", "2"),
            new WellKnownADObject("S-1-18-1", "Authentication Authority Asserted Identity", "2"),
            new WellKnownADObject("S-1-18-2", "Service Asserted Identity", "2"),
            new WellKnownADObject("S-1-18-3", "Fresh public key identity", "2"),
            new WellKnownADObject("S-1-18-4", "Key Trust", "2"),
            new WellKnownADObject("S-1-18-5", "MFA Key Property", "2"),
            new WellKnownADObject("S-1-18-6", "Attested Key Property", "2"),
            new WellKnownADObject("S-1-2", "Local Authority", "2"),
            new WellKnownADObject("S-1-2-0", "Local", "2"),
            new WellKnownADObject("S-1-2-1", "Console Logon", "2"),
            new WellKnownADObject("S-1-3", "Creator Authority", "2"),
            new WellKnownADObject("S-1-3-0", "Creator Owner", "2"),
            new WellKnownADObject("S-1-3-1", "Creator Group", "2"),
            new WellKnownADObject("S-1-3-2", "Creator Owner Server", "2"),
            new WellKnownADObject("S-1-3-3", "Creator Group Server", "2"),
            new WellKnownADObject("S-1-3-4", "Owner Rights", "2"),
            new WellKnownADObject("S-1-4", "Non-unique Authority", "2"),
            new WellKnownADObject("S-1-5", "NT Authority", "2"),
            new WellKnownADObject("S-1-5-1", "Dialup", "2"),
            new WellKnownADObject("S-1-5-10", "Principal Self", "2"),
            new WellKnownADObject("S-1-5-11", "Authenticated Users", "2"),
            new WellKnownADObject("S-1-5-113", "Local Account", "2"),
            new WellKnownADObject("S-1-5-114", "Local Account And Members Of Administrators Group", "2"),
            new WellKnownADObject("S-1-5-12", "Restricted Code", "2"),
            new WellKnownADObject("S-1-5-13", "Terminal Server Users", "2"),
            new WellKnownADObject("S-1-5-14", "Remote Interactive Logon", "2"),
            new WellKnownADObject("S-1-5-15", "This Organization", "2"),
            new WellKnownADObject("S-1-5-19", "Local Service", "2"),
            new WellKnownADObject("S-1-5-2", "Network", "2"),
            new WellKnownADObject("S-1-5-20", "Network Service", "2"),
            new WellKnownADObject("S-1-5-21-0-0-0-496", "Compounded Authentication", "2"),
            new WellKnownADObject("S-1-5-21-0-0-0-497", "Claims Valid", "2"),
            new WellKnownADObject("S-1-5-3", "Batch", "2"),
            new WellKnownADObject("S-1-5-32-545", "Users", "2"),
            new WellKnownADObject("S-1-5-32-546", "Guests", "2"),
            new WellKnownADObject("S-1-5-32-554", "Pre-Windows 2000 Compatible Access", "2"),
            new WellKnownADObject("S-1-5-32-579", "Access Control Assistance Operators", "2"),
            new WellKnownADObject("S-1-5-32-581", "System Managed Accounts Group", "2"),
            new WellKnownADObject("S-1-5-32-583", "Device Owners", "2"),
            new WellKnownADObject("S-1-5-33", "Write Restricted Code", "2"),
            new WellKnownADObject("S-1-5-4", "Interactive", "2"),
            new WellKnownADObject("S-1-5-6", "Service", "2"),
            new WellKnownADObject("S-1-5-64-10", "NTLM Authentication", "2"),
            new WellKnownADObject("S-1-5-64-14", "SChannel Authentication", "2"),
            new WellKnownADObject("S-1-5-64-21", "Digest Authentication", "2"),
            new WellKnownADObject("S-1-5-65-1", "This Organization Certificate", "2"),
            new WellKnownADObject("S-1-5-7", "Anonymous", "2"),
            new WellKnownADObject("S-1-5-8", "Proxy", "2"),
            new WellKnownADObject("S-1-5-80", "NT Service", "2"),
            new WellKnownADObject("S-1-5-80-0", "All Services", "2"),
            new WellKnownADObject("S-1-5-83-0", "NT Virtual Machine\\Virtual Machines", "2"),
            new WellKnownADObject("S-1-5-84-0-0-0-0-0", "User Mode Drivers", "2"),
            new WellKnownADObject("S-1-5-90-0", "Window Manager\\Window Manager Group", "2"),
            new WellKnownADObject("S-1-6", "Site Server Authority", "2"),
            new WellKnownADObject("S-1-7", "Internet Site Authority", "2"),
            new WellKnownADObject("S-1-8", "Exchange Authority", "2"),
            new WellKnownADObject("S-1-9", "Resource Manager Authority", "2"),

            // Well-known Tier 0 RIDs
            new WellKnownADObject("-498", "Enterprise Read-only Domain Controllers", "0"),
            new WellKnownADObject("-500", "Administrator", "0"),
            new WellKnownADObject("-502", "KRBTGT", "0"),
            new WellKnownADObject("-512", "Domain Admins", "0"),
            new WellKnownADObject("-516", "Domain Controllers", "0"),
            new WellKnownADObject("-517", "Cert Publishers", "0"),
            new WellKnownADObject("-520", "Group Policy Creator Owners", "0"),
            new WellKnownADObject("-521", "Read-only Domain Controllers", "0"),
            new WellKnownADObject("-522", "Cloneable Domain Controllers", "0"),
            new WellKnownADObject("-526", "Key Admins", "0"),
            new WellKnownADObject("-527", "Enterprise Key Admins", "0"),
            new WellKnownADObject("-518", "Schema Admins", "0"),
            new WellKnownADObject("-519", "Enterprise Admins", "0"),

            // Well-known Tier 1 RIDs
            new WellKnownADObject("-553", "RAS and IAS Servers", "1"),
            new WellKnownADObject("-572", "Denied RODC Password Replication Group", "1"),

            // Well-known Tier 2 RIDs
            new WellKnownADObject("-501", "Guest", "2"),
            new WellKnownADObject("-513", "Domain Users", "2"),
            new WellKnownADObject("-514", "Domain Guests", "2"),
            new WellKnownADObject("-515", "Domain Computers", "2"),
            new WellKnownADObject("-525", "Protected Users", "2"),
            new WellKnownADObject("-571", "Allowed RODC Password Replication Group", "2"),

            // These groups do not always have the same RID. Some sources say they do, but they don't
            new WellKnownADObject(null, "DnsAdmins", "0"),
            new WellKnownADObject(null, "DnsUpdateProxy", "0"),
            new WellKnownADObject(null, "WinRMRemoteWMIUsers__", "0"),
        };
    }

    public class WellKnownADObject
    {
        public readonly string sidEndsWith;
        public readonly string name;
        public readonly string tier;

        public WellKnownADObject(string sidEndsWith, string name, string tier)
        {
            this.sidEndsWith = sidEndsWith;
            this.name = name;
            this.tier = tier;
        }
    }
}
