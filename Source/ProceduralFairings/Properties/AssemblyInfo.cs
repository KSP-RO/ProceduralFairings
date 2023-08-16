#define CIBUILD_disabled
using System;
using System.Reflection;
using System.Runtime.InteropServices;

//  General Information about the specified assembly.

[assembly: AssemblyTitle ("ProceduralFairings")]
[assembly: AssemblyDescription ("Procedural Fairings for KSP")]
[assembly: AssemblyCopyright ("Copyright © 2016 - 2022, e-dog, rsparkyc, PhineasFreak, KSP-RO group")]
[assembly: AssemblyConfiguration ("Release")]
[assembly: AssemblyProduct ("ProceduralFairings")]

//  Hide the specified assembly from any COM components.

[assembly: ComVisible (false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid ("71c1f577-92c0-4ad6-9d1c-58a6eb02a0bc")]

//  Assembly version information. Consists of the following four values:
//
//    • Major Version
//    • Minor Version
//    • Build Number
//    • Revision

//  The KSPAssembly attribute can be used to ensure that the plugin assemblies
//  are loaded in the correct order.
//  This attribute is not currently used but it is included here for completeness.
[assembly: AssemblyVersion ("6.0.0.0")]
#if CIBUILD
[assembly: AssemblyFileVersion ("@MAJOR@.@MINOR@.@PATCH@.@BUILD@")]
[assembly: KSPAssembly ("ProceduralFairings", @MAJOR@, @MINOR@)]
#else
[assembly: AssemblyFileVersion ("6.3.0.0")]
[assembly: KSPAssembly ("ProceduralFairings", 6, 3)]
#endif
