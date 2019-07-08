using System;
using System.Threading.Tasks;
using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="temporaryWorkSpacePath"></param>
        /// <returns></returns>
        public async Task UnzipAndOrganize(string targetPath, 
            string outputPath, 
            [Option("tmp","Temporary work space. It will be deleted at the end of the program.")]string temporaryWorkSpacePath="./temp")
        {
         
        }
    }
}