using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

static class SongMetaBuilder
{
    public static SongMeta ParseFile(string path, Encoding enc = null)
    {
        using (StreamReader reader = TxtReader.GetFileStreamReader(path, enc))
        {
            bool finishedHeaders = false;
            string directory = new FileInfo(path).Directory.FullName;
            string filename = new FileInfo(path).Name;

            Dictionary<string, string> requiredFields = new Dictionary<string, string>(){
                {"artist", null},
                {"bpm", null},
                {"cover", null},
                {"mp3", null},
                {"title", null}
            };
            Dictionary<string, string> voiceNames = new Dictionary<string, string>();
            Dictionary<string, string> otherFields = new Dictionary<string, string>();

            while (!finishedHeaders || !reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!line.StartsWith("#", StringComparison.Ordinal))
                {
                    finishedHeaders = true;
                    break;
                }
                char[] separator = {':'};
                string[] parts = line.Substring(1).Split(separator, 2);
                if (parts.Length < 2 || parts[0].Length < 1 || parts[1].Length < 1)
                {
                    throw new SongMetaBuilderException("Invalid line formatting on line "+line+" of file "+path);
                }
                string tag = parts[0].ToLower();
                string val = parts[1];

                if (tag.Equals("encoding", StringComparison.Ordinal))
                {
                    if (val.Equals("UTF8", StringComparison.Ordinal))
                    {
                        val = "UTF-8";
                    }
                    Encoding newEncoding = Encoding.GetEncoding(val);
                    if (!newEncoding.Equals(reader.CurrentEncoding))
                    {
                        reader.Dispose();
                        return ParseFile(path, newEncoding);
                    }
                }
                else if (requiredFields.ContainsKey(tag))
                {
                    requiredFields[tag] = val;
                }
                else if (tag.StartsWith("p", StringComparison.Ordinal))
                {
                    if (!voiceNames.ContainsKey(tag.ToUpper()))
                    {
                        voiceNames.Add(tag.ToUpper(), val);
                    }
                    // silently ignore already set voiceNames
                }
                else if (tag.StartsWith("duetsingerp", StringComparison.Ordinal))
                {
                    string shorttag = tag.Substring(10).ToUpper();
                    if (!voiceNames.ContainsKey(shorttag))
                    {
                        voiceNames.Add(shorttag, val);
                    }
                    // silently ignore already set voiceNames
                }
                else
                {
                    if (otherFields.ContainsKey(tag))
                    {
                        throw new SongMetaBuilderException("Cannot set '"+tag+"' twice in file "+path);
                    }
                    otherFields.Add(tag, val);
                }
            }

            // this _should_ get handled by the ArgumentNullException
            // further down below, but that produces really vague
            // messages about a parameter 's' for some reason
            foreach(var item in requiredFields)
            {
                if (item.Value == null)
                {
                    throw new SongMetaBuilderException("Required tag '"+item.Key+"' was not set in file: "+path);
                }
            }


            try {
                SongMeta res = new SongMeta(
                    directory,
                    filename,
                    requiredFields["artist"],
                    ConvertToFloat(requiredFields["bpm"]),
                    requiredFields["cover"],
                    requiredFields["mp3"],
                    requiredFields["title"],
                    voiceNames,
                    reader.CurrentEncoding
                );
                foreach(var item in otherFields)
                {
                    switch(item.Key)
                    {
                        case "background":
                            res.Background = item.Value;
                            break;
                        case "comment":
                            res.Comment = item.Value;
                            break;
                        case "creator":
                            res.Edition = item.Value;
                            break;
                        case "edition":
                            res.Edition = item.Value;
                            break;
                        case "end":
                            res.End = ConvertToFloat(item.Value);
                            break;
                        case "gap":
                            res.End = ConvertToFloat(item.Value);
                            break;
                        case "genre":
                            res.Genre = item.Value;
                            break;
                        case "language":
                            res.Language = item.Value;
                            break;
                        case "source":
                            res.Source = item.Value;
                            break;
                        case "start":
                            res.Start = ConvertToFloat(item.Value);
                            break;
                        case "updated":
                            res.Updated = item.Value;
                            break;
                        case "video":
                            res.Video = item.Value;
                            break;
                        case "videogap":
                            res.VideoGap = ConvertToFloat(item.Value);
                            break;
                        case "year":
                            res.Year = ConvertToUInt32(item.Value);
                            break;
                        // todo: these fields are not really implemented, and should be moved above here once they are
                        case "calcmedley":
                            res.CalcMedley = ConvertToUInt32(item.Value);
                            break;
                        case "medleyendbeat":
                            res.MedleyEndBeat = ConvertToUInt32(item.Value);
                            break;
                        case "medleystartbeat":
                            res.MedleyStartBeat = ConvertToUInt32(item.Value);
                            break;
                        case "notesgap":
                            res.NotesGap = ConvertToUInt32(item.Value);
                            break;
                        case "previewstart":
                            res.PreviewStart = ConvertToUInt32(item.Value);
                            break;
                        case "relative":
                            res.Relative = Convert.ToBoolean(item.Value);
                            break;
                        case "resolution":
                            res.Resolution = item.Value;
                            break;
                        default:
                            throw new SongMetaBuilderException("Unrecognized tag '"+item.Key+"' in file "+path);
                    }
                }
                return res;
            }
            catch (ArgumentNullException e)
            {
                // if you get these with e.ParamName == "s", it's probably one of the non-nullable things (ie, float, uint, etc)
                throw new SongMetaBuilderException("Required tag '"+e.ParamName+"' was not set in file: "+path);
            }
        }
    }

    private static float ConvertToFloat(string s)
    {
        try
        {
            return float.Parse(s, CultureInfo.InvariantCulture.NumberFormat);
        }
        catch (FormatException e)
        {
            throw new SongMetaBuilderException("Could not convert "+s+" to a float. Reason: "+e.Message);
        }
    }

    private static uint ConvertToUInt32(string s)
    {
        try
        {
            return Convert.ToUInt32(s, 10);
        }
        catch (FormatException e)
        {
            throw new SongMetaBuilderException("Could not convert "+s+" to an uint. Reason: "+e.Message);
        }
    }
}

[Serializable]
public class SongMetaBuilderException : Exception
{
    public SongMetaBuilderException()
    {
    }

    public SongMetaBuilderException(string message)
        : base(message)
    {
    }

    public SongMetaBuilderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
