# Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts

``` diff
 namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts {
     public abstract class PatternContext<TFrame> : IPatternContext {
         protected TFrame Frame;
         protected PatternContext();
         public virtual void Declare(Action<IPathSegment, bool> declare);
         protected bool IsStackEmpty();
         public virtual void PopDirectory();
         protected void PushDataFrame(TFrame frame);
         public abstract void PushDirectory(DirectoryInfoBase directory);
         public abstract bool Test(DirectoryInfoBase directory);
         public abstract PatternTestResult Test(FileInfoBase file);
     }
     public abstract class PatternContextLinear : PatternContext<PatternContextLinear.FrameData> {
         public PatternContextLinear(ILinearPattern pattern);
         protected ILinearPattern Pattern { get; }
         protected string CalculateStem(FileInfoBase matchedFile);
         protected bool IsLastSegment();
         public override void PushDirectory(DirectoryInfoBase directory);
         public override PatternTestResult Test(FileInfoBase file);
         protected bool TestMatchingSegment(string value);
         public struct FrameData {
             public bool InStem;
             public bool IsNotApplicable;
             public int SegmentIndex;
             public string Stem { get; }
             public IList<string> StemItems { get; }
         }
     }
     public class PatternContextLinearExclude : PatternContextLinear {
         public PatternContextLinearExclude(ILinearPattern pattern);
         public override bool Test(DirectoryInfoBase directory);
     }
     public class PatternContextLinearInclude : PatternContextLinear {
         public PatternContextLinearInclude(ILinearPattern pattern);
         public override void Declare(Action<IPathSegment, bool> onDeclare);
         public override bool Test(DirectoryInfoBase directory);
     }
     public abstract class PatternContextRagged : PatternContext<PatternContextRagged.FrameData> {
         public PatternContextRagged(IRaggedPattern pattern);
         protected IRaggedPattern Pattern { get; }
         protected string CalculateStem(FileInfoBase matchedFile);
         protected bool IsEndingGroup();
         protected bool IsStartingGroup();
         public override void PopDirectory();
         public sealed override void PushDirectory(DirectoryInfoBase directory);
         public override PatternTestResult Test(FileInfoBase file);
         protected bool TestMatchingGroup(FileSystemInfoBase value);
         protected bool TestMatchingSegment(string value);
         public struct FrameData {
             public bool InStem;
             public bool IsNotApplicable;
             public IList<IPathSegment> SegmentGroup;
             public int BacktrackAvailable;
             public int SegmentGroupIndex;
             public int SegmentIndex;
             public string Stem { get; }
             public IList<string> StemItems { get; }
         }
     }
     public class PatternContextRaggedExclude : PatternContextRagged {
         public PatternContextRaggedExclude(IRaggedPattern pattern);
         public override bool Test(DirectoryInfoBase directory);
     }
     public class PatternContextRaggedInclude : PatternContextRagged {
         public PatternContextRaggedInclude(IRaggedPattern pattern);
         public override void Declare(Action<IPathSegment, bool> onDeclare);
         public override bool Test(DirectoryInfoBase directory);
     }
 }
```

