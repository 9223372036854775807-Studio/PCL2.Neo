using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Runtime.InteropServices;

#pragma warning disable CA1416

namespace PCL2.Neo.Models.Minecraft.JavaSearcher;

internal class Windows
{
    private static Task<JavaExist> PathEnvSearchAsync(string path) => Task.Run(() => new JavaExist
    {
        IsExist = File.Exists(Path.Combine(path, "javaw.exe")), Path = path
    });

    private static async Task<List<JavaEntity>> EnvionmentJavaEntities()
    {
        var javaList = new List<JavaEntity>();

        // find by environment path
        // JAVA_HOME
        var javaHomePath = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (javaHomePath != null || Directory.Exists(javaHomePath)) // if not exist then return
            if (Directory.Exists(javaHomePath))
            {
                //var filePath = javaHomePath.EndsWith(@"\bin\") ? javaHomePath : Path.Combine(javaHomePath, "bin");
                //javaList.Add(new JavaEntity(filePath));

                javaList.Add(new JavaEntity(Path.Combine(javaHomePath, "bin")));
            }

        // PATH multi-thread
        var pathList = new ConcurrentBag<JavaExist>();
        foreach (var item in Environment.GetEnvironmentVariable("Path")!.Split(';'))
            pathList.Add(await PathEnvSearchAsync(item));

        javaList.AddRange(pathList.Where(j => j.IsExist).Select(j => new JavaEntity(j.Path)));

        return javaList;
    }

    private static readonly string[] KeySubFolderWrods =
    [
        "java", "jdk", "env", "环境", "run", "软件", "jre", "mc", "dragon",
        "soft", "cache", "temp", "corretto", "roaming", "users", "craft", "program", "世界", "net",
        "游戏", "oracle", "game", "file", "data", "jvm", "服务", "server", "客户", "client", "整合",
        "应用", "运行", "前置", "mojang", "官启", "新建文件夹", "eclipse", "microsoft", "hotspot",
        "runtime", "x86", "x64", "forge", "原版", "optifine", "官方", "启动", "hmcl", "mod", "高清",
        "download", "launch", "程序", "path", "version", "baka", "pcl", "zulu", "local", "packages",
        "4297127d64ec6", "国服", "网易", "ext", "netease", "1.", "启动"
    ];

    public const int MaxDeep = 7;

    private static List<JavaEntity> SearchFolders(string folderPath, int deep, int maxDeep = MaxDeep)
    {
        // if too deep then return
        if (deep >= maxDeep) return [];

        var entities = new List<JavaEntity>();

        try
        {
            if (File.Exists(Path.Combine(folderPath, "javaw.exe"))) entities.Add(new JavaEntity(folderPath));

            var subFolder = Directory.GetDirectories(folderPath);

            var selectFolder = subFolder.Where(f => KeySubFolderWrods.Any(w => f.ToLower().Contains(w.ToLower())));
            //entities.AddRange(selectFolder.Select(SearchFolders).SelectMany(i => i).ToList());
            foreach (var folder in selectFolder)
                entities.AddRange(SearchFolders(folder, deep + 1)); // search sub folders
        }
        catch (UnauthorizedAccessException)
        {
            // ignore can not access folder
        }

        return entities;
    }

    private static Task<List<JavaEntity>> SearchFoldersAsync(string folderPath, int deep = 0, int maxDeep = MaxDeep) =>
        Task.Run(() => SearchFolders(folderPath, deep, maxDeep));

    private static async Task<List<JavaEntity>> DriveJavaEntities(int maxDeep)
    {
        var javaList = new ConcurrentBag<JavaEntity>();

        var readyDrive = DriveInfo.GetDrives().Where(d => d is { IsReady: true, DriveType: DriveType.Fixed });
        var readyRootFolders = readyDrive.Select(d => d.RootDirectory)
            .Where(f => !f.Attributes.HasFlag(FileAttributes.ReparsePoint));

        // search java start at root folders
        // multi-thread
        foreach (var item in readyRootFolders)
        {
            var entities = await SearchFoldersAsync(item.ToString(), maxDeep: maxDeep);
            foreach (var entity in entities) javaList.Add(entity);
        }

        return javaList.ToList();
    }

    private static List<JavaEntity> RegisterSearch()
    {
        // JavaSoft
        using var javaSoftKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\JavaSoft");
        if (javaSoftKey == null) return [];

        var javaList = new List<JavaEntity>();

        foreach (var subKeyName in javaSoftKey.GetSubKeyNames())
        {
            using var subKey = javaSoftKey.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree);

            var javaHome = subKey?.GetValue("JavaHome");

            var javaHoemPath = javaHome?.ToString();
            if (javaHoemPath == null) continue;

            var exePath = Path.Combine(javaHoemPath, "bin", "javaw.exe");
            if (File.Exists(exePath)) javaList.Add(new JavaEntity(Path.Combine(javaHoemPath, "bin")));
        }

        return javaList;
    }

    public static async Task<List<JavaEntity>> SearchJavaAsync(bool fullSearch = false, int maxDeep = MaxDeep)
    {
        var javaEntities = new List<JavaEntity>();

        javaEntities.AddRange(RegisterSearch()); // search register
        javaEntities.AddRange(await EnvionmentJavaEntities()); // search environment

        if (fullSearch) javaEntities.AddRange(await DriveJavaEntities(maxDeep)); // full search
        else
        {
            string[] searchPath =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Java"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Packages\Microsoft.4297127D64EC6_8wekyb3d8bbwe\LocalCache\Local\runtime")
            ];

            foreach (var item in searchPath)
                if (Directory.Exists(item))
                    javaEntities.AddRange(await SearchFoldersAsync(item, maxDeep: 6));
        }

        return javaEntities;
    }
}

/// <summary>
/// 处理Unix系统下的java
/// </summary>
internal class Unix
{
    public static List<JavaEntity> SearchJavaAsync(PlatformID platform)
    {
        string[] searchPath;

        // make search path by platform
        switch (platform)
        {
            case PlatformID.Unix:
                searchPath =
                [
                    "/usr/lib/jvm",
                    "/usr/java",
                    "/opt"
                ];
                break;
            case PlatformID.MacOSX:
                searchPath =
                [
                    "/Library/Java/JavaVirtualMachines",
                    "/usr/local/Caskroom",
                    "/usr/local/opt/openjdk",
                    "/opt"
                ];
                break;
            default:
                return [];
        }

        var javaList = new List<JavaEntity>();

        foreach (var item in searchPath)
        {
            // ignore if not exist
            if (!Directory.Exists(item)) continue;

            try
            {
                javaList.AddRange(Directory.EnumerateDirectories(item)
                    .Select(jvmDir => new { jvmDir, javaExecutable = FindJavaExecuteable(jvmDir) })
                    .Where(@t => !string.IsNullOrEmpty(@t.javaExecutable))
                    .Select(@t => new JavaEntity(@t.jvmDir)));
            }
            catch (UnauthorizedAccessException)
            {
                // ignore
            }
        }

        return javaList;
    }

    private static string? FindJavaExecuteable(string jvmPath)
    {
        string[] possiblePaths =
        [
            Path.Combine(jvmPath, "bin", "java"),
            Path.Combine(jvmPath, "Contents", "Home", "bin", "java")
        ];

        return possiblePaths.FirstOrDefault(File.Exists);
    }
}