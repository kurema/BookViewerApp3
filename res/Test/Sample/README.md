# About files
| name | password | encryption |
| -- | -- | -- | -- |
| encrypted.pass.pdf | pass | 40-bit RC4 | Basic password. |
| encrypted.brabra.pdf | brabra | 256-bit AES | |
| encrypted.hahaha.pdf | hahaha | 128-bit AES | |
| encrypted.ownerpass.pdf | ownerpass (owner password) | 256-bit AES | No user password. With owner password. |
| sample.pdf | | | The original |

* "pass" may be in password dictionary.
* 256-bit AES seems to be not supported by Windows.Data.Pdf.PdfDocument.
