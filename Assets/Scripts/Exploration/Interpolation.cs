﻿using UnityEngine;
using Assets.Scripts.Extensions;

namespace Assets.Scripts.Exploration
{
    /// <summary>
    /// Takes care of calculation of interpolations
    /// Taken and adapted from exercised done for a course in SBV with Prof. Zwettler
    /// </summary>
    public static class Interpolation
    {
        public static Color GetNearestNeighbourInterpolation(Texture2D originalImage, int width, int height, double tgtX, double tgtY, bool isBackgroundColored)
        {
            (int xPos, int yPos) coordinate = getCoordinatePosition(width, height, tgtX, tgtY, isBackgroundColored);

            if (isBackgroundColored)
            {
                return getBackgroundIfInvalid(originalImage, width, height, coordinate.xPos, coordinate.yPos);
            }
            return originalImage.GetPixel(coordinate.xPos, coordinate.yPos);
        }

        public static Color GetBiLinearInterpolatedValue(Texture2D inImg, int width, int height, double targetX, double targetY, bool isBackgroundColored)
        {
            (int xPos, int yPos) coordinate = getCoordinatePosition(width, height, targetX, targetY, isBackgroundColored);
            int xPos = coordinate.xPos;
            int yPos = coordinate.yPos;

            if (isBackgroundColored && (xPos < 0 || xPos >= width || yPos < 0 || yPos >= height))
            {
                return new Color();
            }

            double deltaX = targetX - xPos;
            double deltaY = targetY - yPos;
            var originValue = inImg.GetPixel(xPos, yPos);

            //take care of borders
            if (xPos + 1 == width || yPos + 1 == height)
            {
                return originValue;
            }

            var xValue = inImg.GetPixel(xPos + 1, yPos);
            var yValue = inImg.GetPixel(xPos, yPos + 1);
            var xyValue = inImg.GetPixel(xPos + 1, yPos + 1);

            var x1 = originValue.ToArgb() + (xValue.ToArgb() - originValue.ToArgb()) * deltaX;
            var x2 = yValue.ToArgb() + (xyValue.ToArgb() - yValue.ToArgb()) * deltaX;
            var value = x1 + (x2 - x1) * deltaY + 0.5;

            return ColorExtensions.FromArgb((int)value);
        }
       
        private static (int xPos, int yPos) getCoordinatePosition(int width, int height, double tgtX, double tgtY, bool allowInvalid)
        {
            int xPos = (int)(tgtX + 0.5);
            int yPos = (int)(tgtY + 0.5);

            if (allowInvalid)
            {
                return (xPos, yPos);
            }

            if (xPos < 0)
            {
                xPos = 0;
            }
            else if (xPos >= width)
            {
                xPos = width - 1;
            }

            if (yPos < 0)
            {
                yPos = 0;
            }
            else if (yPos >= height)
            {
                yPos = height - 1;
            }

            return (xPos, yPos);
        }

        private static Color getBackgroundIfInvalid(Texture2D inImg, int width, int height, int xPos, int yPos)
        {
            if (xPos >= 0 && xPos < width && yPos >= 0 && yPos < height)
            {
                return inImg.GetPixel(xPos, yPos);
            }
            return new Color();
        }

    }
}
