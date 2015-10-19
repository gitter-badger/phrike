﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;

namespace DataAccess
{
    public class UnitOfWork : IDisposable
    {
        private readonly OperationPhrikeContext _context = new OperationPhrikeContext();



        private BaseEntityRepository<PositionData> _positionDataRepository;
        private BaseEntityRepository<Propositus> _propositusRepository;
        private BaseEntityRepository<Survey> _surveyRepository;
        private BaseEntityRepository<SurveyQuestion> _surveyQuestionRepository;
        private BaseEntityRepository<SurveyResult> _surveyResultRepository;
        private BaseEntityRepository<Test> _testRepository;
        private BaseEntityRepository<Video> _videoRepository;

        public BaseEntityRepository<PositionData> PositionDataRepository
        {
            get
            {
                if (_positionDataRepository == null)
                {
                    _positionDataRepository = new BaseEntityRepository<PositionData>(_context);
                }
                return _positionDataRepository;
            }
        }

        public BaseEntityRepository<Propositus> PropositusRepository
        {
            get
            {
                if (_propositusRepository == null)
                {
                    _propositusRepository = new BaseEntityRepository<Propositus>(_context);
                }
                return _propositusRepository;
            }
        }

        public BaseEntityRepository<Survey> SurveyRepository
        {
            get
            {
                if (_surveyRepository == null)
                {
                    _surveyRepository = new BaseEntityRepository<Survey>(_context);
                }
                return _surveyRepository;
            }
        }

        public BaseEntityRepository<SurveyQuestion> SurveyQuestionRepository
        {
            get
            {
                if (_surveyQuestionRepository == null)
                {
                    _surveyQuestionRepository = new BaseEntityRepository<SurveyQuestion>(_context);
                }
                return _surveyQuestionRepository;
            }
        }

        public BaseEntityRepository<SurveyResult> SurveyResultRepository
        {
            get
            {
                if (_surveyResultRepository == null)
                {
                    _surveyResultRepository = new BaseEntityRepository<SurveyResult>(_context);
                }
                return _surveyResultRepository;
            }
        }

        public BaseEntityRepository<Test> TestRepository
        {
            get
            {
                if (_testRepository == null)
                {
                    _testRepository = new BaseEntityRepository<Test>(_context);
                }
                return _testRepository;
            }
        }

        public BaseEntityRepository<Video> VideoRepository
        {
            get
            {
                if (_videoRepository == null)
                {
                    _videoRepository = new BaseEntityRepository<Video>(_context);
                }
                return _videoRepository;
            }
        }


        public void Save()
        {
            _context.SaveChanges();
        }

        private bool _disposed = false;
        public void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this._disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
