/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Native Interface
* @copyright 2018-2021 AXELL Corporation
* @date July 28, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin Native Interface
* @copyright 2018-2021 AXELL Corporation
* @date July 28, 2021
*/
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

    /**
    * \~japanese
    * RGBA順
    * 
    * \~english
    * RGBA order
    */
    public const Int32  AILIA_IMAGE_FORMAT_RGBA      = (0x00);
    /**
    * \~japanese
    * BGRA順
    * 
    * \~english
    * BGRA order
    */
    public const Int32  AILIA_IMAGE_FORMAT_BGRA      = (0x01);

    /**
    * \~japanese
    * RGBA順(Bottom to Top)
    * 
    * \~english
    * RGBA order (Bottom to Top)
    */
    public const Int32  AILIA_IMAGE_FORMAT_RGBA_B2T  = (0x10);
    /**
    * \~japanese
    * BGRA順(Bottom to Top)
    * 
    * \~english
    * BGRA order (Bottom to Top)
    */
    public const Int32  AILIA_IMAGE_FORMAT_BGRA_B2T  = (0x11);

    /****************************************************************
    * ネットワーク画像フォーマット
    **/

    /**
    * \~japanese
    * BGR順
    * 
    * \~english
    * BGR order
    */
    public const Int32   AILIA_NETWORK_IMAGE_FORMAT_BGR               = (0);
    /**
    * \~japanese
    * RGB順
    * 
    * \~english
    * RGB oreder
    */
    public const Int32   AILIA_NETWORK_IMAGE_FORMAT_RGB               = (1);
    /**
    * \~japanese
    * Gray Scale (1ch)
    * 
    * \~english
    * Gray Scale (1ch)
    */
    public const Int32   AILIA_NETWORK_IMAGE_FORMAT_GRAY              = (2);
    /**
    * \~japanese
    * ヒストグラム平坦化 Gray Scale (1ch)
    * 
    * \~english
    * Histogram Flattening Gray Scale (1ch)
    */
    public const Int32   AILIA_NETWORK_IMAGE_FORMAT_GRAY_EQUALIZE     = (3);

    /**
    * \~japanese
    * DCYX順
    * 
    * \~english
    * DYXC order
    */
    public const Int32   AILIA_NETWORK_IMAGE_CHANNEL_FIRST            = (0);
    /**
    * \~japanese
    * DYXC順
    * 
    * \~english
    * DYXC order
    */
    public const Int32   AILIA_NETWORK_IMAGE_CHANNEL_LAST             = (1);

    /**
    * \~japanese
    * 0 - 255
    * 
    * \~english
    * 0 - 255
    */
    public const Int32   AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_INT8      = (0);
    /**
    * \~japanese
    * -128 - 127
    * 
    * \~english
    * -128 - 127
    */
    public const Int32   AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8        = (1);
    /**
    * \~japanese
    * 0.0 - 1.0
    * 
    * \~english
    * 0.0 - 1.0
    */
    public const Int32   AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32      = (2);
    /**
    * \~japanese
    * -1.0 - 1.0
    * 
    * \~english
    * -1.0 - 1.0
    */
    public const Int32   AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32        = (3);
    /**
    * \~japanese
    * ImageNet mean&std normalization
    * 
    * \~english
    * ImageNet mean&std normalizatio
    */
    public const Int32   AILIA_NETWORK_IMAGE_RANGE_IMAGENET           = (4);

    /**
    * \~japanese
    * @brief 画像のフォーマットを変換します。
    * @param dst                  変換後画像の格納先(numeric型-stride * height * チャンネル数(解説参照)以上のサイズを確保すること)
    * @param dst_width            変換後画像の横幅
    * @param dst_height           変換後画像の高さ
    * @param dst_format           変換後画像の形式 (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param dst_channel          変換後画像のチャンネル順 (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param dst_range            変換後画像のレンジ (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param src                  変換元画像の格納先(32bpp)
    * @param src_stride           変換元画像のラインバイト数
    * @param src_width            変換元画像の横幅
    * @param src_height           変換元画像の高さ
    * @param src_format           変換元画像の形式 (AILIA_IMAGE_FORMAT_*)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   画像フォーマットを変更します。dst_formatが \ref AILIA_NETWORK_IMAGE_FORMAT_BGR もしくはAILIA_NETWORK_IMAGE_FORMAT_RGB
    *   の場合、チャンネル数は3,  \ref AILIA_NETWORK_IMAGE_FORMAT_GRAY の場合チャンネル数は1となります。
    * 
    * \~english
    * @brief   Convert image format.
    * @param dst           Where to store the converted image (type-numeric-stride * height * must be larger than the number of channels (see explanation))
    * @param dst_width     Width of the converted image
    * @param dst_height    Height of converted image
    * @param dst_forma     Format of converted image (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param dst_channel   Channel   order of the converted image (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param dst_range     Range of the converted image (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param src           Destination of converted source image (32bpp)
    * @param src_stride    Number of line bytes in the source image
    * @param src_width     Width of the source image
    * @param src_height    Height of the source image
    * @param src_format    Format of the source image (AILIA_IMAGE_FORMAT_*)
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns an error code.
    * @details
    *   Change the image format, if dst_format is \ref AILIA_NETWORK_IMAGE_FORMAT_BGR or AILIA_NETWORK_IMAGE_FORMAT_RGB
    *   if dst_format is \ref AILIA_NETWORK_IMAGE_FORMAT_BGR or AILIA_NETWORK_IMAGE_FORMAT_RGB, the number of channels is 3, if dst_format is \ref AILIA_NETWORK_IMAGE_FORMAT_GRAY the number of channels is 1.
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaFormatConvert(IntPtr dst, UInt32 dst_width, UInt32 dst_height, UInt32 dst_format, UInt32 dst_channel, UInt32 dst_range, IntPtr src, Int32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format);
}
