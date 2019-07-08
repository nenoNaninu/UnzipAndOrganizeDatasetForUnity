using System;
using System.Linq;

namespace UnzipAndOrganizeDatasetForUnity
{
    public static class Extensions
    {
        /// <summary>
        /// スネークケースをアッパーキャメル(パスカル)ケースに変換します
        /// 例) quoted_printable_encode → QuotedPrintableEncode
        /// </summary>
        public static string SnakeToUpperCamel( this string self )
        {
            if ( string.IsNullOrEmpty( self ) ) return self;

            return self
                    .Split( new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => char.ToUpperInvariant( s[ 0 ] ) + s.Substring( 1, s.Length - 1 ) )
                    .Aggregate( string.Empty, ( s1, s2 ) => s1 + s2 )
                ;
        }
    }
}