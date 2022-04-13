/* AILIA Unity Plugin Native Interface */
//* Copyright 2018-2021 AXELL CORPORATION */
/* Updated July 28, 2021*/

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class AiliaFormat
{
    /****************************************************************
    * 入力画像形式
    **/

    public const UInt32  AILIA_IMAGE_FORMAT_RGBA      = (0x00); //RGBA順
    public const UInt32  AILIA_IMAGE_FORMAT_BGRA      = (0x01); //BGRA順

    public const UInt32  AILIA_IMAGE_FORMAT_RGBA_B2T  = (0x10); //RGBA順(Bottom to Top)
    public const UInt32  AILIA_IMAGE_FORMAT_BGRA_B2T  = (0x11); //BGRA順(Bottom to Top)

    /****************************************************************
    * ネットワーク画像フォーマット
    **/

    public const UInt32   AILIA_NETWORK_IMAGE_FORMAT_BGR               = (0);   //BGR順
    public const UInt32   AILIA_NETWORK_IMAGE_FORMAT_RGB               = (1);   //RGB順
    public const UInt32   AILIA_NETWORK_IMAGE_FORMAT_GRAY              = (2);   //Gray Scale (1ch)
    public const UInt32   AILIA_NETWORK_IMAGE_FORMAT_GRAY_EQUALIZE     = (3);   //ヒストグラム平坦化 Gray Scale (1ch)

    public const UInt32   AILIA_NETWORK_IMAGE_CHANNEL_FIRST            = (0);   //DCYX順
    public const UInt32   AILIA_NETWORK_IMAGE_CHANNEL_LAST             = (1);   //DYXC順

    public const UInt32   AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_INT8      = (0);   //0 - 255
    public const UInt32   AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8        = (1);   //-128 - 127
    public const UInt32   AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32      = (2);   //0.0 - 1.0
    public const UInt32   AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32        = (3);   //-1.0 - 1.0
    public const UInt32   AILIA_NETWORK_IMAGE_RANGE_IMAGENET           = (4);   //ImageNet mean&std normalization

    /**
    *  画像のフォーマットを変換します。
    *    引数:
    *      dst                  - 変換後画像の格納先(numeric型-stride * height * チャンネル数(解説参照)以上のサイズを確保すること)
    *      dst_width            - 変換後画像の横幅
    *      dst_height           - 変換後画像の高さ
    *      dst_format           - 変換後画像の形式 (AILIA_NETWORK_IMAGE_FORMAT_*)
    *      dst_channel          - 変換後画像のチャンネル順 (AILIA_NETWORK_IMAGE_CHANNEL_*)
    *      dst_range            - 変換後画像のレンジ (AILIA_NETWORK_IMAGE_RANGE_*)
    *      src                  - 変換元画像の格納先(32bpp)
    *      src_stride           - 変換元画像のラインバイト数
    *      src_width            - 変換元画像の横幅
    *      src_height           - 変換元画像の高さ
    *      src_format           - 変換元画像の形式 (AILIA_IMAGE_FORMAT_*)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      画像フォーマットを変更します。dst_formatがAILIA_NETWORK_IMAGE_FORMAT_BGRもしくはAILIA_NETWORK_IMAGE_FORMAT_RGB
    *      の場合、チャンネル数は3, AILIA_NETWORK_IMAGE_FORMAT_GRAYの場合チャンネル数は1となります。
    */

    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaFormatConvert(IntPtr dst, UInt32 dst_width, UInt32 dst_height, UInt32 dst_format, UInt32 dst_channel, UInt32 dst_range, IntPtr src, Int32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format);
}
