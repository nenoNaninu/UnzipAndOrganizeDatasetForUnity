using System;
using System.IO;
using System.Threading.Tasks;
using MicroBatchFramework;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace UnzipAndOrganizeDatasetForUnity
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await BatchHost.CreateDefaultBuilder().RunBatchEngineAsync<MainBatch>(args);
        }
    }

    public class MainBatch : BatchBase
    {
        private static bool CreateDirectory(string checkPath)
        {
            if (!Directory.Exists(checkPath))
            {
                Directory.CreateDirectory(checkPath);
                return true;
            }

            return false;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
        
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static void CopyObjectDirectory(string[] objectFilePaths, string outputPath)
        {
            foreach (var objectFilePath in objectFilePaths)
            {
                var dirName = Path.GetFileName(Path.GetDirectoryName(objectFilePath)).SnakeToUpperCamel();
                dirName = Regex.Replace(dirName, @"^[0-9]*", "");
                var index = dirName.IndexOf("model", StringComparison.CurrentCultureIgnoreCase);

                //Model 〜〜みたいな感じで始まるやつはModelを削除する。
                if (index == 0)
                {
                    dirName = dirName.Substring(5);
                }

                var parentDirName = Regex.Replace(dirName, @"[0-9]", "");
                parentDirName = Regex.Replace(parentDirName, "(.$)+", "");

                CreateDirectory(Path.Join(outputPath, parentDirName));

                var finalOutputPath = Path.Join(outputPath, parentDirName, dirName);

                if (!Directory.Exists(finalOutputPath))
                {
                    DirectoryCopy(Path.GetDirectoryName(objectFilePath), finalOutputPath, true);
                }
            }
        }

        private static void ExtractZip(string[] zipFilePaths, string temporaryWorkSpacePath, string outputPath)
        {
            foreach (var zipFilePath in zipFilePaths)
            {
                //解凍して
                var outputZipPath = Path.Join(temporaryWorkSpacePath, Path.GetFileNameWithoutExtension(zipFilePath));
                ZipFile.ExtractToDirectory(zipFilePath, outputZipPath, true);

                //obj or zip が入っているかを見る
                var objFiles = Directory.GetFiles(outputZipPath, "*.obj", SearchOption.AllDirectories);
                var zipFiles = Directory.GetFiles(outputZipPath, "*.zip", SearchOption.AllDirectories);

                //zipがあれば
                if (zipFiles != null && zipFiles.Length >= 1)
                {
                    ExtractZip(zipFiles, temporaryWorkSpacePath, outputPath);
                }

                //objがあればoutputPathに展開済みを吐き出す。
                if (objFiles != null && objFiles.Length >= 1)
                {
                    CopyObjectDirectory(objFiles, outputPath);
                    Console.WriteLine($"{outputZipPath} done");
                }
            }
        }

        /// <summary>
        /// もとのフォルダには影響を与えないように。
        /// 
        /// フォルダを全探索してzipだったら解凍。
        /// ファイル名はすべてアッパーキャメルに一度直してから処理を行う。
        /// zipの中身が.objを含んでいたらoutputPathにそのzip名から
        /// 数字と拡張子と"Model"を抜いたものを名前として持つディレクトリに。
        /// 数字を削除していないやつを保存する。
        /// 例)
        /// Model_Noodle_1.0.zipにたいなのがあったら、
        /// outputPath/Noodle/Noodle1.0/model.objになる感じ。
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="temporaryWorkSpacePath"></param>
        /// <returns></returns>
        public void UnzipAndOrganize([Option(0)] string targetPath, [Option(1)] string outputPath,
            [Option("tmp", "Temporary work space. It will be deleted at the end of the program.")]
            string temporaryWorkSpacePath = "tempWorkSpace")
        {
            temporaryWorkSpacePath += $"{DateTime.Now:yyyyMMddHHmmss}";
            try
            {
                if (!CreateDirectory(temporaryWorkSpacePath) || !CreateDirectory(outputPath))
                {
                    DeleteDirectory(temporaryWorkSpacePath);
                    DeleteDirectory(outputPath);
                    return;
                }

                //zipファイル全部集めてくる。
                string[] zipFilePaths = Directory.GetFiles(targetPath, "*.zip", SearchOption.AllDirectories);
                string[] objFilePaths = Directory.GetFiles(targetPath, "*.obj", SearchOption.AllDirectories);

                CopyObjectDirectory(objFilePaths, outputPath);
                ExtractZip(zipFilePaths, temporaryWorkSpacePath, outputPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("==============");
                Console.WriteLine(e.Message);
                Console.WriteLine(e);
                DeleteDirectory(outputPath);
            }
            finally
            {
                DeleteDirectory(temporaryWorkSpacePath);
            }
        }
    }
}