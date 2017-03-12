# WaveAnalyzer

## About

This project was orignally a project from when I was in school. I decided to upload this ti GitHub
incase I ever wanted to revisied it. It is very much presented as is and I take no repsonsibility 
it you download this and it causes problems.

### What does it do?

This is program will take in a .wav file and analyze the data display the waveform and
fourier distribution.

### Supported formats

*.wav* Mono and Stereo

## Usage

### Record your own wave file

#### File > New

This will open a dialog that will allow you to record a new wave
file to use, can also preview the file in the is dialog, clicking 
*End* will return you to the Main window

#### Import

#### File > Import

or click "Import" button

This will allow users to select a wave file to be used.

#### File > Export

or click "Export" button

This export the current project as a *.wav* file

### Known Issues

- It is threaded *TERRIBLY* and will likely task your CPU
- The performance is awful, mainly due to the fact that its is mostly a brute force approch
- The original Read me that the Program points to, when missing at some point,
  so you will get an error if you attempt to open it from Help > view read me 