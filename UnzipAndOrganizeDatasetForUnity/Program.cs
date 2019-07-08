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
        private bool CreateDirectory(string checkPath)
        {
            if (!Directory.Exists(checkPath))
            {
                Directory.CreateDirectory(checkPath);
                return true;
            }

            return false;
        }

        private void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path,true);
            }
        }

        private void ZipExtract(string[] zipFilePaths, string temporaryWorkSpacePath, string outputPath)
        {
            foreach (var zipFilePath in zipFilePaths)
            {
                //解凍して
                var outputZipPath = Path.Join(temporaryWorkSpacePath, Path.GetFileNameWithoutExtension(zipFilePath));
                ZipFile.ExtractToDirectory(zipFilePath, outputZipPath,true);

                //obj or zip が入っているかを見る
                var objFiles = Directory.GetFiles(outputZipPath, "*.obj", SearchOption.AllDirectories);
                var zipFiles = Directory.GetFiles(outputZipPath, "*.zip", SearchOption.AllDirectories);

                //zipがあれば
                if (zipFiles != null && zipFiles.Length >= 1)
                {
                    ZipExtract(zipFiles, temporaryWorkSpacePath, outputPath);
                }

                //objがあればoutputPathに展開済みを吐き出す。
                if (objFiles != null && objFiles.Length == 1)
                {
                    var dirName = Path.GetFileNameWithoutExtension(zipFilePath).SnakeToUpperCamel();
//                    dirName = Regex.Replace(dirName, @"^[0-9]", "");
                    var index = dirName.IndexOf("model", StringComparison.CurrentCultureIgnoreCase);

                    //Model 〜〜みたいな感じで始まるやつはModelを削除する。
                    if (index == 0)
                    {
                        dirName = dirName.Substring(5);
                    }

                    var parentDirName = Regex.Replace(dirName, @"[0-9]", "");
                    CreateDirectory(Path.Join(outputPath, parentDirName));
                    var outputPathFinally = Path.Join(outputPath, parentDirName, dirName);
                    if (!Directory.Exists(outputPathFinally))
                    {
                        Directory.GetParent(objFiles[0]).MoveTo(outputPathFinally);
                    }

                    Console.WriteLine($"{outputZipPath} done");
                }
            }

//            Parallel.ForEach(zipFilePaths, zipFilePath =>
//            {
//                //解凍して
//                var outputZipPath = temporaryWorkSpacePath + Path.GetFileName(zipFilePath);
//                ZipFile.ExtractToDirectory(zipFilePath, outputZipPath);
//
//                //objが入っているか見る。
//                var objFiles = Directory.GetFiles(zipFilePath, "*.obj");
//
//                //ディレクトリの中にzipファイルがある場合、解凍して中身を確かめないといけない。
//                var zipFiles = Directory.GetFiles(zipFilePath, "*.zip");
//                if (zipFiles != null && zipFiles.Length >= 1)
//                {
//                    ZipExtract(zipFiles, temporaryWorkSpacePath, outputPath);
//                }
//
//                //存在していればoutputPathに吐き出す。
//                if (objFiles != null && objFiles.Length >= 1)
//                {
//                    var dirName = Path.GetFileName(zipFilePath).SnakeToUpperCamel();
//                    var index = dirName.IndexOf("Model", StringComparison.CurrentCultureIgnoreCase);
//
//                    //Model 〜〜みたいな感じで始まるやつはModelを削除する。
//                    if (index == 0)
//                    {
//                        dirName = dirName.Substring(5);
//                    }
//
//                    var parentDirName = Regex.Replace(dirName, @"[0-9]", "");
//                    CreateDirectory(Path.Join(outputPath, parentDirName));
//
//                    Directory.Move(outputZipPath, Path.Join(outputPath, parentDirName, dirName));
//                    Console.WriteLine($"{outputZipPath} done");
//                }
//            });
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

                ZipExtract(zipFilePaths, temporaryWorkSpacePath, outputPath);
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