using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTextSharp.text.pdf;
public class PdfReaderPasswordTester : PdfReader
{
	public PdfReaderPasswordTester(Stream isp, bool forceRead = true)
	{
		Tokens = new PrTokeniser(new RandomAccessFileOrArray(isp, forceRead));
	}

	public void TestPassword(byte[] ownerPassword)
	{
		Password = ownerPassword;
		ReadPdf();
	}
}
