using DotSpatial.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfectHeightMapDownload
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Way1 (Provide only Geographic extent)
            var feature = FeatureSet.Open(@"D:\3DTerrainInfoGenerationProject\BuildingProject\Resource\Extent.shp");
            int zoom = 11;
            string output_folder = @"D:\3DTerrainInfoGenerationProject\BuildingProject\Resource\HeightData";
            int image_dimension = 1080;//square shaped image
            Console.WriteLine($"Required Extent: {feature.Extent.MinX} {feature.Extent.MinY} {feature.Extent.MaxX} {feature.Extent.MaxY}");
            HeightMapCreator heightMapCreator = new HeightMapCreator(
                zoom, feature.Extent.MinX, feature.Extent.MinY,
                feature.Extent.MaxX, feature.Extent.MaxY,
                output_folder, image_dimension
                );

            if (heightMapCreator.getHeightMap())
                Console.WriteLine("Done!");
            #endregion

            #region Way2 (Provide Centre of Geographic extent & req. distance in KM)
            //int zoom = 11;
            //string output_folder = @"D:\3DTerrainData\HeightDEM\Test";
            //int image_dimension = 1080;//square shaped image
            //var extnt = GeometryHelper.GetExtentFromCenterCoordinate(new DotSpatial.Topology.Coordinate(91.89006, 25.35083), 10);
            ////GeometryHelper.GetExtentFromCenterCoordinate(new DotSpatial.Topology.Coordinate(90.39079, 23.75118), 10);
            //Console.WriteLine($"Required Extent: {GeometryHelper.Extent.MinX} {GeometryHelper.Extent.MinY} {GeometryHelper.Extent.MaxX} {GeometryHelper.Extent.MaxY}");
            //GeometryHelper.CreateExtentShapeFile(extnt, @"D:\3DTerrainData\HeightDEM\Test\Extent.shp");
            //HeightMapCreator heightMapCreator = new HeightMapCreator(
            //    zoom, GeometryHelper.Extent.MinX, GeometryHelper.Extent.MinY,
            //    GeometryHelper.Extent.MaxX, GeometryHelper.Extent.MaxY,
            //    output_folder, image_dimension
            //    );
            //if (heightMapCreator.getHeightMap())
            //    Console.WriteLine("Done!");
            #endregion
        }
    }
}
