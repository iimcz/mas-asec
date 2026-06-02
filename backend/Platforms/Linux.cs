using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace asec.Platforms;

public static class Linux
{
    public static List<string> GetDisplays()
    {
        return Directory.GetDirectories("/sys/class/drm/").Select(e => Path.GetDirectoryName(e)).ToList();
    }

    public static async Task<string> Execute(bool sudo, string program, List<string> args, CancellationToken cancellationToken = default)
    {
        var executable = sudo ? "sudo" : program;
        if (sudo)
            args.Insert(0, program);

        ProcessStartInfo startInfo = new(executable, args)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode == 0)
                return process.StandardOutput.ReadToEnd();
            else
                return process.StandardError.ReadToEnd();
        }
        return null;
    }

    public static async Task<string> QemuImg(bool sudo, List<string> args, CancellationToken cancellationToken = default)
        => await Execute(sudo, "qemu-img", args, cancellationToken);

    public static async Task<string> QemuNbd(bool sudo, List<string> args, CancellationToken cancellationToken = default)
        => await Execute(sudo, "qemu-nbd", args, cancellationToken);

    public static async Task<string> Mkfs(bool sudo, List<string> args, CancellationToken cancellationToken = default)
        => await Execute(sudo, "mkfs", args, cancellationToken);

    public static async Task<string> Mount(bool sudo, List<string> args, CancellationToken cancellationToken = default)
        => await Execute(sudo, "mount", args, cancellationToken);

    public static async Task<string> Umount(bool sudo, List<string> args, CancellationToken cancellationToken = default)
        => await Execute(sudo, "umount", args, cancellationToken);

    public static async Task<string> MakeQcow2Image(long sizeBytes, string path, FileSystem fs = FileSystem.Ext4, CancellationToken cancellationToken = default)
    {
        // NOTE: could be 1024^2 but this will suffice for now...
        var sizeMB = sizeBytes / 1000_000 + 1;
        sizeMB += 3; // For ext4 header and structures

        StringWriter sw = new();

        var partial = await QemuImg(false, ["create", "-f", "qcow2", path, $"{sizeMB}M"], cancellationToken);
        sw.WriteLine(partial);

        // TODO: find first available nbd device, instead of hardcoded 0
        partial = await QemuNbd(true, ["--connect=/dev/nbd0", path], cancellationToken);
        sw.WriteLine(partial);

        partial = await Mkfs(true, ["-t", fs.ToString().ToLower(), "-F", "/dev/nbd0", "-E", "root_perms=666"]);
        sw.WriteLine(partial);

        partial = await QemuNbd(true, ["-d", "/dev/nbd0"], cancellationToken);
        sw.WriteLine(partial);

        return sw.ToString();
    }

    public static async Task<string> MountQcow2Image(string path, string mountpoint, CancellationToken cancellationToken = default)
    {
        StringWriter sw = new();

        var partial = await QemuNbd(true, ["--connect=/dev/nbd0", path], cancellationToken);
        sw.WriteLine(partial);

        // NOTE: assumes the whole image is formatted without a partition table
        partial = await Mount(true, [mountpoint, "/dev/nbd0"], cancellationToken);
        sw.WriteLine(partial);

        return sw.ToString();
    }

    public static async Task<string> UnmountQcow2Image(string mountpoint, CancellationToken cancellationToken = default)
    {
        StringWriter sw = new();

        var partial = await Umount(true, [mountpoint], cancellationToken);
        sw.WriteLine(partial);

        // TODO: check which device was actually mounted to the mountpoint and disconnect that
        partial = await QemuNbd(true, ["-d", "/dev/nbd0"], cancellationToken);
        sw.WriteLine(partial);

        return sw.ToString();
    }

    public static async Task<JsonDocument> ReadImageInfo(string path, CancellationToken cancellationToken = default)
    {
        var output = await QemuImg(false, ["info", "--output", "json", path], cancellationToken);
        return JsonDocument.Parse(output); // TODO: error checking
    }

    public static async Task<string> FlattenQcow2Image(string topLayerPath, string outputFile, CancellationToken cancellationToken = default)
    {
        return await QemuImg(false, ["convert", "-f", "qcow2", "-O", "qcow2", topLayerPath, outputFile], cancellationToken);
    }

    public static async Task<bool> PollDisplayConnected(string display)
    {
        using var reader = new StreamReader($"/sys/class/drm/{display}/status", new FileStreamOptions() {
            Access = FileAccess.Read
        });
        var status = await reader.ReadLineAsync() ?? "";
        status = status.Trim();

        return status == "connected";
    }
     
    [DllImport("libc", SetLastError=true, EntryPoint="kill")]
    private static extern int sys_kill (int pid, int sig);
    
    public static void Kill(this Process process, Signum sig)
    {
        sys_kill(process.Id, (int) sig);
    }
}

public enum FileSystem
{
    Ext4
}
    
public enum Signum : int
{
    SIGHUP    =  1, // Hangup (POSIX).
    SIGINT    =  2, // Interrupt (ANSI).
    SIGQUIT   =  3, // Quit (POSIX).
    SIGILL    =  4, // Illegal instruction (ANSI).
    SIGTRAP   =  5, // Trace trap (POSIX).
    SIGABRT   =  6, // Abort (ANSI).
    SIGIOT    =  6, // IOT trap (4.2 BSD).
    SIGBUS    =  7, // BUS error (4.2 BSD).
    SIGFPE    =  8, // Floating-point exception (ANSI).
    SIGKILL   =  9, // Kill, unblockable (POSIX).
    SIGUSR1   = 10, // User-defined signal 1 (POSIX).
    SIGSEGV   = 11, // Segmentation violation (ANSI).
    SIGUSR2   = 12, // User-defined signal 2 (POSIX).
    SIGPIPE   = 13, // Broken pipe (POSIX).
    SIGALRM   = 14, // Alarm clock (POSIX).
    SIGTERM   = 15, // Termination (ANSI).
    SIGSTKFLT = 16, // Stack fault.
    SIGCLD    = SIGCHLD, // Same as SIGCHLD (System V).
    SIGCHLD   = 17, // Child status has changed (POSIX).
    SIGCONT   = 18, // Continue (POSIX).
    SIGSTOP   = 19, // Stop, unblockable (POSIX).
    SIGTSTP   = 20, // Keyboard stop (POSIX).
    SIGTTIN   = 21, // Background read from tty (POSIX).
    SIGTTOU   = 22, // Background write to tty (POSIX).
    SIGURG    = 23, // Urgent condition on socket (4.2 BSD).
    SIGXCPU   = 24, // CPU limit exceeded (4.2 BSD).
    SIGXFSZ   = 25, // File size limit exceeded (4.2 BSD).
    SIGVTALRM = 26, // Virtual alarm clock (4.2 BSD).
    SIGPROF   = 27, // Profiling alarm clock (4.2 BSD).
    SIGWINCH  = 28, // Window size change (4.3 BSD, Sun).
    SIGPOLL   = SIGIO, // Pollable event occurred (System V).
    SIGIO     = 29, // I/O now possible (4.2 BSD).
    SIGPWR    = 30, // Power failure restart (System V).
    SIGSYS    = 31, // Bad system call.
    SIGUNUSED = 31
}
