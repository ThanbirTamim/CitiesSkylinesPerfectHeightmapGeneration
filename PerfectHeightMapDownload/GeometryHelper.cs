using DotSpatial.Data;
using DotSpatial.Projections;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfectHeightMapDownload
{
    public static class GeometryHelper
    {
        public static Extent Extent = new Extent();
        public static List<Coordinate> GetExtentFromCenterCoordinate(Coordinate center, double distance_km)
        {
            double hypotenuse_each_quardilator = Math.Sqrt(Math.Pow((distance_km / 2), 2) + Math.Pow((distance_km / 2), 2)); //Hy = Sqrt(L2 + h2)
            double rad_angle = (Math.PI / 180) * 45;
            double distance = hypotenuse_each_quardilator / 100;

            //left-top
            double x1 = center.X - distance * Math.Cos(rad_angle);
            double y1 = center.Y + distance * Math.Sin(rad_angle);
            //right-top
            double x2 = center.X + distance * Math.Cos(rad_angle);
            double y2 = center.Y + distance * Math.Sin(rad_angle);
            //right-bottom
            double x3 = center.X + distance * Math.Cos(rad_angle);
            double y3 = center.Y - distance * Math.Sin(rad_angle);
            //left-bottom
            double x4 = center.X - distance * Math.Cos(rad_angle);
            double y4 = center.Y - distance * Math.Sin(rad_angle);

            Extent.MinX = x4;
            Extent.MinY = y4;
            Extent.MaxX = x2;
            Extent.MaxY = y2;

            List<Coordinate> extent_area = new List<Coordinate>();
            extent_area.Add(new Coordinate(x1, y1));//left-top
            extent_area.Add(new Coordinate(x2, y2));//right-top
            extent_area.Add(new Coordinate(x3, y3));//right-bottom
            extent_area.Add(new Coordinate(x4, y4));//left-bottom

            return extent_area;
        }

        public static bool CreateExtentShapeFile(List<Coordinate> coordinates, string shape_file_name, string text_file_name = null)
        {
            try
            {
                FeatureSet fs = new FeatureSet(FeatureType.Polygon);
                fs.Projection = ProjectionInfo.FromEpsgCode(4326);

                Coordinate[] coord = new Coordinate[]
                {
                coordinates[0],
                coordinates[1],
                coordinates[2],
                coordinates[3],
                coordinates[0]
                };

                fs.Features.Add(new Polygon(new LinearRing(coord)));
                fs.SaveAs(shape_file_name, true);

                if (text_file_name == null)
                    File.WriteAllText(Path.GetDirectoryName(shape_file_name) + @"/Extent.txt", $"{coordinates[3].X},{coordinates[3].Y},{coordinates[1].X},{coordinates[1].Y}");
                else
                    File.WriteAllText(text_file_name, $"{coordinates[3].X},{coordinates[3].Y},{coordinates[1].X},{coordinates[1].Y}");
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static bool CreateExtentShapeFile(double minx, double miny, double maxx, double maxy, string shape_file_name)
        {
            List<Coordinate> extent_area = new List<Coordinate>();
            extent_area.Add(new Coordinate(minx, maxy));//left-top
            extent_area.Add(new Coordinate(maxx, maxy));//right-top
            extent_area.Add(new Coordinate(maxx, miny));//right-bottom
            extent_area.Add(new Coordinate(minx, miny));//left-bottom
            CreateExtentShapeFile(extent_area, shape_file_name);
            return true;
        }
    }
}
