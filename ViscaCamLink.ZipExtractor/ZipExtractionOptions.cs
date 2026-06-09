namespace ViscaCamLink.ZipExtractor;

using System;

public class ZipExtractionOptions
{
    public ZipExtractionOptions() 
    { 
        var args = Environment.GetCommandLineArgs();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();

            switch(arg)
            {
                case ArgumentInput:
                    InputPath = args[i + 1];
                    break;
                case ArgumentOutput:
                    OutputPath = args[i + 1];
                    break;
                case ArgumentOwnerExe:
                    OwnerExecutablePath = args[i + 1];
                    break;
                case ArgumentExtractedExe:
                    ExtractedExecutableFileName = args[i + 1];
                    break;
                case ArgumentClear:
                    ClearOutputPath = true;
                    break;
            }
        }

        if (String.IsNullOrWhiteSpace(InputPath) || 
            String.IsNullOrWhiteSpace(OutputPath) || 
            String.IsNullOrWhiteSpace(OwnerExecutablePath))
        {
            throw new MissingFieldException($"Start argument is missing. Required arguments: {ArgumentInput}, {ArgumentOutput}, {ArgumentOwnerExe}");
        }
    }

    private const String ArgumentInput = "--input";
    private const String ArgumentOutput = "--output";
    private const String ArgumentOwnerExe = "--owner-exe";
    private const String ArgumentExtractedExe = "--extracted-exe";
    private const String ArgumentClear = "--clear";

    public String InputPath { get; }

    public String OutputPath { get; }

    public String OwnerExecutablePath { get; }

    public String? ExtractedExecutableFileName { get; }

    public Boolean ClearOutputPath { get; }
}
