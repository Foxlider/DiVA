# ------------------------------------------------------------
# Setup Environment
# ------------------------------------------------------------
PATH=/usr/bin:/bin
umask 022
PDIR=${0%`basename $0`}
ZIP_FILENAME=linux-x64.zip

# Number of lines in this script file (plus 1)
SCRIPT_LINES=64

# Run /bin/sum on your binary and put the two values here
SUM1=07636
SUM2=99688
echo PATH
echo PDIR

echo "Unpacking binary files..."
#tail -n +$SCRIPT_LINES "$0" &gt; ${PDIR}/${ZIP_FILENAME}

SUM=`sum ${PDIR}/${ZIP_FILENAME}` 
ASUM1=`echo "${SUM}" | awk '{print $1}'`
ASUM2=`echo "${SUM}" | awk '{print $2}'`
if [ ${ASUM1} -ne ${SUM1} ] || [ ${ASUM2} -ne ${SUM2} ]; then
  echo "The download file appears to be corrupted. Please download"
  echo "the file again and re-try the installation."
  exit 1
fi