SET objDir=%1
SET binDir=%2

mkdir %objDir%
mkdir %binDir%

cl /c /Fo%objDir%\aspnetcorev2_arm64.obj empty.cpp
cl /c /arm64EC /Fo%objDir%\aspnetcorev2_x64.obj empty.cpp

link /lib /machine:arm64 /def:aspnetcorev2_arm64.def /out:%objDir%\aspnetcorev2_arm64.lib
link /lib /machine:x64 /def:aspnetcorev2_x64.def /out:%objDir%\aspnetcorev2_x64.lib

link /dll /noentry /machine:arm64x /defArm64Native:aspnetcorev2_arm64.def /def:aspnetcorev2_x64.def %objDir%\aspnetcorev2_arm64.obj %objDir%\aspnetcorev2_x64.obj /out:%binDir%\aspnetcorev2.dll %objDir%\aspnetcorev2_arm64.lib %objDir%\aspnetcorev2_x64.lib

cl /c /Fo%objDir%\aspnetcorev2_outofprocess_arm64.obj empty.cpp
cl /c /arm64EC /Fo%objDir%\aspnetcorev2_outofprocess_x64.obj empty.cpp

link /lib /machine:arm64 /def:aspnetcorev2_outofprocess_arm64.def /out:%objDir%\aspnetcorev2_outofprocess_arm64.lib
link /lib /machine:x64 /def:aspnetcorev2_outofprocess_x64.def /out:%objDir%\aspnetcorev2_outofprocess_x64.lib

link /dll /noentry /machine:arm64x /defArm64Native:aspnetcorev2_outofprocess_arm64.def /def:aspnetcorev2_outofprocess_x64.def %objDir%\aspnetcorev2_outofprocess_arm64.obj %objDir%\aspnetcorev2_outofprocess_x64.obj /out:%binDir%\aspnetcorev2_outofprocess.dll %objDir%\aspnetcorev2_outofprocess_arm64.lib %objDir%\aspnetcorev2_outofprocess_x64.lib
