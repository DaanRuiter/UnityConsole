# UnityConsole
Command Console for Unity.

Be sure to call ```CMD.Init(GameObject);``` before utilizing the console.
To add custom commands simply use the ConsoleCommand attribute as follows:
```C#
[ConsoleCommand("Hello!")]
public static void CMDHello ()
{
  Log("Hello World!");
}
```
Please note that the method needs to be both static & public.
It must also begin with CMD, this way you can have both the normal method and a static command version of the same method.

Make sure that you give a call ```CMD.RefreshCommands``` with a reference to all your assemblies containing commands before calling ```CMD.Init().```
If you are not using the DLL, this is probably not needed.
