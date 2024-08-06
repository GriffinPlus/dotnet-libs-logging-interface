using System.Reflection;

[assembly: AssemblyCompany("Griffin+")]
[assembly: AssemblyProduct("Griffin+ Logging")]
[assembly: AssemblyCopyright("Copyright (c) 2024 Sascha Falk and Contributors")]
[assembly: AssemblyVersion("1.1.2.0")]
[assembly: AssemblyFileVersion("1.1.2.0")]
[assembly: AssemblyInformationalVersion("1.1.2-ci.1+Branch.master.Sha.16510ff3152522123a0e43b3eb130ed3b1b2b241")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
