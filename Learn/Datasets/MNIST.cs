﻿//
// Code to download and load the MNIST data.
//

using System;
using System.IO;
using System.IO.Compression;
using Mono;
using TensorFlow;

namespace Learn.Mnist
{
	public struct MnistImage
	{
		public int Cols, Rows;
		public byte [] Data;

		public MnistImage (int cols, int rows, byte [] data)
		{
			Cols = cols;
			Rows = rows;
			Data = data;
		}
	}

	public class Mnist 
	{
		public MnistImage [] TrainImages, TestImages, ValidationImages;
		public byte [] TrainLabels, TestLabels, ValidationLabels;
		public byte [,] OneHotTrainLabels, OneHotTestLabels, OneHotValidationLabels;

		int Read32 (Stream s)
		{
			var x = new byte [4];
			s.Read (x, 0, 4);
			return DataConverter.BigEndian.GetInt32 (x, 0);
		}

		MnistImage [] ExtractImages (Stream input, string file)
		{
			using (var gz = new GZipStream (input, CompressionMode.Decompress)) {
				if (Read32 (gz) != 2051)
					throw new Exception ("Invalid magic number found on the MNIST " + file);
				var count = Read32 (gz);
				var rows = Read32 (gz);
				var cols = Read32 (gz);

				var result = new MnistImage [count];
				for (int i = 0; i < count; i++) {
					var size = rows * cols;
					var data = new byte [size];
					gz.Read (data, 0, size);

					result [i] = new MnistImage (cols, rows, data);
				}
				return result;
			}
		}


		byte [] ExtractLabels (Stream input, string file)
		{
			using (var gz = new GZipStream (input, CompressionMode.Decompress)) {
				if (Read32 (gz) != 2049)
					throw new Exception ("Invalid magic number found on the MNIST " + file);
				var count = Read32 (gz);
				var labels = new byte [count];
				gz.Read (labels, 0, count);

				return labels;
			}
		}

		T [] Pick<T> (T [] source, int first, int last)
		{
			if (last == 0)
				last = source.Length;
			var count = last - first;
			var result = new T [count];
			Array.Copy (source, first, result, 0, count);
			return result;
		}

		// Turn the labels array that contains values 0..numClasses-1 into
		// a One-hot encoded array
		byte [,] OneHot (byte [] labels, int numClasses)
		{
			var oneHot = new byte [labels.Length, numClasses];
			for (int i = 0; i < labels.Length; i++) {
				oneHot [i, labels [i]] = 1;
			}
			return oneHot;
		}

		/// <summary>
		/// Reads the data sets.
		/// </summary>
		/// <param name="trainDir">Directory where the training data is downlaoded to.</param>
		/// <param name="numClasses">Number classes to use for one-hot encoding, or zero if this is not desired</param>
		/// <param name="validationSize">Validation size.</param>
		public void ReadDataSets (string trainDir, int numClasses = 0, int validationSize = 5000)
		{
			const string SourceUrl = "http://yann.lecun.com/exdb/mnist/";
			const string TrainImagesName = "train-images-idx3-ubyte.gz";
			const string TrainLabelsName = "train-labels-idx1-ubyte.gz";
			const string TestImagesName = "t10k-images-idx3-ubyte.gz";
			const string TestLabelsName = "t10k-labels-idx1-ubyte.gz";

			TrainImages = ExtractImages (Helper.MaybeDownload (SourceUrl, trainDir, TrainImagesName), TrainImagesName);
			TestImages  = ExtractImages (Helper.MaybeDownload (SourceUrl, trainDir, TestImagesName), TestImagesName);
			TrainLabels = ExtractLabels (Helper.MaybeDownload (SourceUrl, trainDir, TrainLabelsName), TrainLabelsName);
			TestLabels = ExtractLabels (Helper.MaybeDownload (SourceUrl, trainDir, TestLabelsName), TestLabelsName);

			ValidationImages = Pick (TrainImages, 0, validationSize);
			ValidationLabels = Pick (TrainLabels, 0, validationSize);
			TrainImages = Pick (TrainImages, validationSize, 0);
			TrainLabels = Pick (TrainLabels, validationSize, 0);

			if (numClasses != -1) {
				OneHotTrainLabels = OneHot (TrainLabels, numClasses);
				OneHotValidationLabels = OneHot (ValidationLabels, numClasses);
				OneHotTestLabels = OneHot (TestLabels, numClasses);
			}
		}
	}
}
