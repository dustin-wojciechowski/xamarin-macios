using System.IO;

public class BlockContainer : CodeBlock {
	public BlockContainer (int currentIndent) : base (currentIndent)
	{
		Indent = 0;
	}

	public override void Print (StreamWriter writer)
	{
		foreach (ICodeBlock block in Blocks) {
			block.SetIndent (CurrentIndent);
			block.Print (writer);
		}
	}

	public void SetIndent (int indent)
	{
		CurrentIndent = indent;
	}
}
